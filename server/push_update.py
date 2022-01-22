#!/usr/bin/env python3

from genericpath import exists
import json
import hashlib
from json.decoder import JSONDecodeError
import os
from argparse import ArgumentParser
from dacite import from_dict
from dataclasses import dataclass, asdict
from packaging.version import parse as parse_version
from shutil import copy2
from typing import List, Dict


@dataclass
class ConfigMod:
    name: str
    torrents: List[str]


@dataclass
class Config:
    base_url: str
    base_path: str
    torrent_path: str
    mods: Dict[str, ConfigMod]


@dataclass
class Resource:
    url: str
    sha256: str

    def update_hash(self):
        self.sha256 = sha256sum(url_to_path(self.url))


@dataclass
class Mod:
    name: str
    latest: str
    addons: Resource

    def update(self, config_mod: ConfigMod):
        resources: List[Resource] = []
        for torrent in config_mod.torrents:
            torrent_path = os.path.join(os.path.join(config.base_path, 'mods', torrent))
            copy2(os.path.join(config.torrent_path, torrent), torrent_path)
            resources.append(Resource(path_to_url(torrent_path), sha256sum(torrent_path)))

        mod_path = os.path.join(config.base_path, 'mods', f'{args.mod}.json')
        write_json(resources, mod_path)

        self.addons.update_hash()


@dataclass
class Locator:
    mods: Resource
    motd: Resource
    servers: Resource

    def update_hashes(self):
        self.mods.update_hash()
        self.servers.update_hash()


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


def sha256sum(file: str) -> str:
    hash = hashlib.sha256()
    b  = bytearray(128 * 1024)
    mv = memoryview(b)
    with open(file, 'rb', buffering=False) as f:
        for n in iter(lambda : f.readinto(mv), 0):
            hash.update(mv[:n])
    return hash.hexdigest()


def write_json(obj: dataclass, file: str):
    with open(file, 'w') as f:
        json.dump(asdict(obj), f, indent=4)


def write_json(obj: object, file: str):
    with open(file, 'w') as f:
        json.dump(obj, f, default=lambda x: x.__dict__, indent=4)


if __name__ == '__main__':
    parser = ArgumentParser(description='Push updates for DayZ2')
    parser.add_argument('-v', '--version', type=str, required=True, help='Version number of the update')
    parser.add_argument('-u', '--update-hashes', action='store_true', required=False, default=False, help='Update hashes of all locator urls')
    parser.add_argument('-m', '--mod', type=str, required=True, help='Mod name of the update')
    parser.add_argument('-o', '--overwrite', action='store_true', required=False, default=False,
        help='Overwrite version if it already exists')
    args = parser.parse_args()

    if not is_valid_version(args.version):
        print('Versions need to be of format 0.0.0.0')
        exit(1)

    config: Config = from_dict(data_class=Config, data=json.load(open('push_update.config', 'r')))

    mods_path = os.path.join(config.base_path, 'mods.json')
    try:
        mods: Dict[str, Mod] = json.load(open(mods_path, 'r'))
    except Exception:
        mods: Dict[str, Mod] = {}

    if not args.mod in config.mods:
        print('There exists no mod with that name in the config, please create it first')
        exit(1)

    config_mod: ConfigMod = config.mods[args.mod]
    
    # TODO: delete extra mods

    if not args.mod in mods:
        print(f'Adding new mod {args.mod} to the webserver')

        mod_path = os.path.join(config.base_path, 'mods', f'{args.mod}.json')
        mod: Mod = Mod(config_mod.name, args.version, Resource(path_to_url(mod_path), sha256sum(mod_path)))
        mod.update(config_mod)
        mods[args.mod] = mod
    else:
        mod: Mod = from_dict(data_class=Mod, data=mods[args.mod])
        if mod.latest == args.version and not args.overwrite:
            print(f'Version {args.version} already is latest version and --overwrite not specified, aborting')
            exit(1)

        mod.name = config_mod.name
        mod.latest = args.version
        mod.update(config_mod)
        mods[args.mod] = mod

    mods.update()
    write_json(mods, mods_path)

    locator_path = os.path.join(config.base_path, 'locator.json')
    locator: Locator = from_dict(data_class=Locator, data=json.load(open(locator_path, 'r')))
    locator.update_hashes()
    write_json(locator, locator_path)
