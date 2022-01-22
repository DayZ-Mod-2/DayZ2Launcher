#!/usr/bin/env python3

import json
import math
import os
import shlex
from argparse import ArgumentParser
from dataclasses import dataclass
from subprocess import PIPE, Popen
from typing import List


@dataclass
class Torrent:
    name: str
    files: List[str]


@dataclass
class Config:
    base: str
    tracker: str
    output: str


def hash_file(file: str) -> str:
    p = Popen(['sha256sum', file], stdout=PIPE, stderr=PIPE, universal_newlines=True)
    stdout, stderr = p.communicate()
    if p.returncode:
        print('ERROR')
        print(f'Failed to hash file: {stderr}')
        exit(1)
    return stdout.split(' ')[0]


def create_archive(torrent: Torrent, config: Config) -> str:
    output = os.path.join(config.output, f'{torrent.name}.7z')

    if os.path.isfile(output):
        os.remove(output)

    #-mmt2
    args = shlex.split(f'7z a {output} {" ".join(torrent.files)}')
    p = Popen(args, cwd=config.base, stdout=PIPE, stderr=PIPE, universal_newlines=True)

    # regex = r"\d{2}(\d{1})?%"
    # matches = re.findall(regex, p.stdout.read(), re.MULTILINE)
    # if len(matches) == 1:
    #     print(matches[0])

    _, stderr = p.communicate()
    if p.returncode:
        print('ERROR')
        print(stderr)
        exit(1)
    else:
        print(f'{"SUCCESS":20}', end=' ', flush=True)
        return output


def create_torrent(torrent: Torrent, archive: str, config: Config):
    torrent_name = os.path.join(config.output, f'{torrent.name}.torrent')

    if os.path.isfile(torrent_name):
        os.remove(torrent_name)

    args = shlex.split(f'mktorrent -v -l {piece_length([archive])} -a {config.tracker} -o {torrent_name} {archive}')
    p = Popen(args, stdout=PIPE, stderr=PIPE, universal_newlines=True)
    _, stderr = p.communicate()
    if p.returncode:
        print('ERROR')
        print(stderr)
        exit(1)
    else:
        print('SUCCESS', flush=True)


def piece_length(files: List[str]) -> int:
    """
    Files up to 50MiB: 32KiB piece size (-l 15)
    Files 50MiB to 150MiB: 64KiB piece size (-l 16)
    Files 150MiB to 350MiB: 128KiB piece size (-l 17)
    Files 350MiB to 512MiB: 256KiB piece size (-l 18)
    Files 512MiB to 1.0GiB: 512KiB piece size (-l 19)
    Files 1.0GiB to 2.0GiB: 1024KiB piece size (-l 20)
    Files 2.0GiB and up: 2048KiB piece size (-l 21)
    """

    piece_limits = {
        50 * 1024 * 1024: 15,
        150 * 1024 * 1024: 16,
        350 * 1024 * 1024: 17,
        512 * 1024 * 1024: 18,
        1024 * 1024 * 1024: 19,
        2048 * 1024 * 1024: 20,
        math.inf: 21,
    }

    total_size = 0
    for file in files:
        total_size += os.path.getsize(file)

    for upper_limit, piece_length in piece_limits.items():
        if total_size <= upper_limit:
            return piece_length


if __name__ == '__main__':
    parser = ArgumentParser(description='Create torrents for DayZ2')
    parser.add_argument('-f', '--force', action='store_true', default=False,
        help='Forces recreation of torrent files', required=False)
    args = parser.parse_args()

    config_file = open('create_torrents.config', 'r')
    j = json.load(config_file)
    torrents = [Torrent(**torrent_json) for torrent_json in j['torrents']]
    config = Config(j['base'], j['tracker'], j['output'])

    print(f'{"Name":20} {"Archive":20} {"CreateTorrent":20}')

    for torrent in torrents:
        print(f'{torrent.name:20}', end=" ", flush=True)

        changed = False
        hashes = []
        for file in torrent.files:
            file = os.path.join(config.base, file)
            basename = os.path.basename(file)
            basepath = file.split(basename)[0]
            hash_file_name = os.path.join(basepath, f'.{basename}.sha256')

            old_hash = ''
            if os.path.isfile(hash_file_name):  # check if a hash exists
                old_hash = open(hash_file_name, 'r').read()

            hash = hash_file(file)
            hashes.append((hash_file_name, hash))
            if hash != old_hash:
                changed = True

        if not args.force and not changed:
            print('NOT CHANGED')
            continue

        archive = create_archive(torrent, config)
        create_torrent(torrent, archive, config)

        # save the new hashes only if the build succeeded
        for file_name, hash in hashes:
            file = open(file_name, 'w').write(hash)
