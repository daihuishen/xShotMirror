"""Preserve upstream notices omitted from MSYS2 binary packages."""
import io
import json
from pathlib import Path, PurePosixPath
import re
import subprocess
import tarfile

ROOT = Path(__file__).resolve().parents[1]


def is_notice(name):
    base = PurePosixPath(name).name
    return bool(re.match(r'(?i)^(copying|copyright|license|licence|notice|authors|patents)([._-].*)?$', base))


def save_notice(destination, name, content):
    relative = PurePosixPath(name)
    if relative.is_absolute() or '..' in relative.parts:
        raise ValueError(f'Unsafe archive member: {name}')
    target = destination.joinpath(*relative.parts)
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(content)


def collect(publish, msys):
    packages = json.loads((publish / 'runtime-packages.json').read_text())
    archiver = msys / 'usr/bin/bsdtar.exe'
    for package in packages:
        if package['license_files']:
            continue
        archive = ROOT / '.tools/source-cache' / package['source_archive']
        names = subprocess.check_output([str(archiver), '-tf', str(archive)], text=True).splitlines()
        dest = publish / 'licenses' / package['name'] / 'upstream-source'
        count = 0
        for name in names:
            if name.endswith('/'):
                continue
            if is_notice(name):
                content = subprocess.check_output([str(archiver), '-xOf', str(archive), name])
                save_notice(dest, name, content)
                count += 1
            elif re.search(r'\.tar\.(gz|xz|bz2)$|\.tgz$', name):
                content = subprocess.check_output([str(archiver), '-xOf', str(archive), name])
                with tarfile.open(fileobj=io.BytesIO(content)) as nested:
                    for member in nested:
                        if member.isfile() and is_notice(member.name):
                            save_notice(dest, member.name, nested.extractfile(member).read())
                            count += 1
        if count == 0:
            # x264's source package carries a bare Git repository rather than a tarball.
            if package['base'] in ('mingw-w64-x264', 'mingw-w64-rtmpdump'):
                project = package['base'].removeprefix('mingw-w64-')
                repo_prefix = f'{package["base"]}/{project}/'
                repo = ROOT / '.tools/license-git' / project
                (repo / 'refs').mkdir(parents=True, exist_ok=True)
                for name in names:
                    if name.startswith(repo_prefix) and not name.endswith('/'):
                        content = subprocess.check_output([str(archiver), '-xOf', str(archive), name])
                        save_notice(repo, name[len(repo_prefix):], content)
                commit = (re.search(r'\.([0-9a-f]{7,40})-\d+$', package['version']).group(1)
                          if project == 'x264' else 'v' + package['version'].rsplit('-', 1)[0])
                git = ['git', '-c', f'safe.directory={repo.as_posix()}', f'--git-dir={repo}']
                paths = subprocess.check_output(git + ['ls-tree', '-r', '--name-only', commit], text=True).splitlines()
                for path in paths:
                    if is_notice(path):
                        content = subprocess.check_output(git + ['show', f'{commit}:{path}'])
                        save_notice(dest, path, content)
                        count += 1
            else:
                raise RuntimeError(f'No upstream notices recovered for {package["name"]}')
        print(f'{package["name"]}: {count} upstream notice files', flush=True)


if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument('--publish', type=Path, default=ROOT / 'artifacts/publish')
    parser.add_argument('--msys', type=Path, default=Path('C:/msys64'))
    args = parser.parse_args()
    collect(args.publish, args.msys)
