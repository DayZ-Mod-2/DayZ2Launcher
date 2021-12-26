#!/usr/bin/env python3

import json
import hashlib
import os
from argparse import ArgumentParser
from dacite import from_dict
from dataclasses import dataclass, asdict
from packaging.version import parse as parse_version
from shutil import copy2
from typing import List

@dataclass
class ConfigGameType:
    torrents: List[str]
    ident: str
    launchable: bool
    name: str


@dataclass
class Config:
    base_url: str
    base_path: str
    mods_path: str
    torrent_path: str
    gametypes: List[ConfigGameType]


@dataclass
class FileInfo:
    url: str
    sha1: str

    def update_hash(self):
        self.sha1 = sha1sum(url_to_path(self.url))


@dataclass
class Addon:
    name: str
    torrent: FileInfo
    version: str


@dataclass
class GameType:
    addons: List[str]
    ident: str
    launchable: bool
    name: str


@dataclass
class Plugin:
    ident: str
    addon: str
    name: str
    description: str


@dataclass
class Mod:
    addons: List[Addon]
    gametypes: List[GameType]
    plugins: List[Plugin]
    version: str


@dataclass
class ModInfo:
    archive: FileInfo
    version: str


@dataclass
class Mods:
    mods: List[ModInfo]
    latest: str

    def update(self):
        for root, dirs, files in os.walk(os.path.join(config.base_path, config.mods_path)):
            for file in files:
                if file == 'mod.json':
                    mod_path = os.path.join(root, file)
                    url = path_to_url(mod_path)

                    mod: Mod = from_dict(data_class=Mod, data=json.load(open(mod_path, 'r')))

                    exists = any([m for m in self.mods if m.version == mod.version])
                    if not exists:
                        self.mods.append(ModInfo(FileInfo(url, sha1sum(mod_path)), mod.version))
        
        latest = ''
        for mod in self.mods:
            mod_path = url_to_path(mod.archive.url)
            if not os.path.isfile(mod_path):
                print(f'Mod {mod_path} does not exist, either add it or remove it from the list')
                exit(1)
            if latest == '' or parse_version(mod.version) > parse_version(latest):
                latest = mod.version
            mod.archive.update_hash()
        self.latest = latest


@dataclass
class Locator:
    mods: FileInfo
    motd: str
    patches: FileInfo
    servers: str

    def update_hashes(self):
        self.mods.update_hash()
        self.patches.update_hash()


def create_mod(version_dir: str, version: str) -> Mod:
    addons: List[Addon] = []
    gametypes: List[GameType] = []

    if not os.path.isdir(version_dir):
        os.mkdir(version_dir)

    for gametype in config.gametypes:
        addon_names: List[str] = []
        for torrent in gametype.torrents:
            addon_name = torrent.split('.torrent')[0]
            addon_names.append(addon_name)
            torrent_path = os.path.join(version_dir, torrent)

            if not os.path.isdir(version_dir):
                os.mkdir(version_dir)

            copy2(os.path.join(config.torrent_path, torrent), torrent_path)

            torrent_url = path_to_url(torrent_path)
            addons.append(Addon(addon_name, FileInfo(torrent_url, sha1sum(torrent_path)), version))

        gametypes.append(GameType(addon_names, gametype.ident, gametype.launchable, gametype.name))

    plugins: List[Plugin] = []
    # TODO: plugins maybe?

    return Mod(addons, gametypes, plugins, version)


def url_to_path(url: str) -> str:
    return url.replace(config.base_url, config.base_path)


def path_to_url(path: str) -> str:
    return path.replace(config.base_path, config.base_url)


def is_valid_version(version: str) -> bool:
    try:
        s = version.split('.')
        [int(x) for x in s]
        return len(s) == 4
    except Exception:
        return False


def sha1sum(file: str) -> str:
    hash = hashlib.sha1()
    b  = bytearray(128 * 1024)
    mv = memoryview(b)
    with open(file, 'rb', buffering=False) as f:
        for n in iter(lambda : f.readinto(mv), 0):
            hash.update(mv[:n])
    return hash.hexdigest()


def write_json(obj: dataclass, file: str):
    with open(file, 'w') as f:
        json.dump(asdict(obj), f, indent=4)


if __name__ == '__main__':
    parser = ArgumentParser(description='Push updates for DayZ2')
    parser.add_argument('-v', '--version', type=str, required=True, help='Version number of the update')
    parser.add_argument('-o', '--overwrite', action='store_true', required=False, default=False,
        help='Overwrite version if it already exists')
    args = parser.parse_args()

    if not is_valid_version(args.version):
        print('Versions need to be of format 0.0.0.0')
        exit(1)

    config: Config = from_dict(data_class=Config, data=json.load(open('push_update.config', 'r')))

    version_dir = os.path.join(config.base_path, config.mods_path, args.version)

    if (not args.overwrite) and os.path.isdir(version_dir):
        print('This version already exists')
        exit(1)

    mod: Mod = create_mod(version_dir, args.version)
    write_json(mod, os.path.join(version_dir, 'mod.json'))

    mods_path = os.path.join(config.base_path, 'mods.json')
    mods: Mods = from_dict(data_class=Mods, data=json.load(open(mods_path, 'r')))
    mods.update()
    write_json(mods, mods_path)

    locator_path = os.path.join(config.base_path, 'locator.json')
    locator: Locator = from_dict(data_class=Locator, data=json.load(open(locator_path, 'r')))
    locator.update_hashes()
    write_json(locator, locator_path)
