"""Bundle the PE dependency closure, not an entire MSYS2 installation."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess


def collect(msys: Path, output: Path, receiver: Path):
    prefix = msys / 'ucrt64'
    binary_dir = prefix / 'bin'
    plugins = ['coreelements', 'app', 'videoparsersbad', 'libav',
               'videoconvertscale', 'd3d11', 'playback', 'autodetect',
               'audioconvert', 'audioresample', 'volume', 'level', 'wasapi']
    pending = [(receiver, output / 'receiver' / receiver.name)]
    pending += [(prefix / 'lib/gstreamer-1.0' / f'libgst{p}.dll',
                 output / 'receiver/plugins' / f'libgst{p}.dll') for p in plugins]
    for source in [prefix / 'libexec/gstreamer-1.0/gst-plugin-scanner.exe',
                   binary_dir / 'gst-inspect-1.0.exe', binary_dir / 'gst-launch-1.0.exe']:
        pending.append((source, output / 'receiver' / source.name))

    owners = {}
    packages = {}
    for directory in (msys / 'var/lib/pacman/local').iterdir():
        if not (directory / 'files').exists():
            continue
        fields = {}
        for block in (directory / 'desc').read_text(encoding='utf-8').split('\n\n'):
            lines = block.splitlines()
            if len(lines) > 1:
                fields[lines[0].strip('%')] = '\n'.join(lines[1:])
        packages[directory.name] = (directory, fields)
        for name in (directory / 'files').read_text(encoding='utf-8').splitlines():
            if name.startswith('ucrt64/') and not name.endswith('/'):
                owners[name.lower()] = directory.name

    visited = set()
    manifest = []
    used_packages = set()
    while pending:
        source, target = pending.pop()
        if source in visited:
            continue
        visited.add(source)
        if not source.is_file():
            raise RuntimeError(f'Missing runtime file: {source}')
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
        owner = None
        if source.is_relative_to(msys):
            owner = owners.get(source.relative_to(msys).as_posix().lower())
            if not owner:
                raise RuntimeError(f'Cannot identify package for {source}')
            used_packages.add(owner)
        manifest.append({'file': target.relative_to(output).as_posix(), 'package': owner or 'UxPlay',
                         'sha256': hashlib.sha256(target.read_bytes()).hexdigest()})
        imports = subprocess.check_output([str(binary_dir / 'objdump.exe'), '-p', str(source)],
                                          text=True, errors='replace')
        for name in re.findall(r'DLL Name:\s+(\S+)', imports):
            library = binary_dir / name
            if library.exists():
                pending.append((library, output / 'receiver' / name))
            elif name.lower().startswith(('api-ms-', 'ext-ms-')):
                continue
            elif not (Path(os.environ['SystemRoot']) / 'System32' / name).is_file():
                raise RuntimeError(f'Unresolved dependency: {name} required by {source}')

    licenses = output / 'licenses'
    licenses.mkdir(exist_ok=True)
    package_manifest = []
    for package in sorted(used_packages):
        directory, fields = packages[package]
        records = (directory / 'files').read_text(encoding='utf-8').splitlines()
        license_files = [name for name in records if not name.endswith('/') and
                         ('/licenses/' in name or re.search(r'/(COPYING[^/]*|LICENSE[^/]*)$', name, re.I))]
        for name in license_files:
            source = msys / name
            if source.is_file():
                target = licenses / package / name
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source, target)
        package_manifest.append({'name': fields['NAME'], 'version': fields['VERSION'],
                                 'base': fields['BASE'], 'license': fields.get('LICENSE', ''),
                                 'upstream': fields.get('URL', ''), 'license_files': license_files,
                                 'source_archive': f"{fields['BASE']}-{fields['VERSION']}.src.tar.zst"})
    common = msys / 'usr/share/licenses/common'
    if common.exists():
        shutil.copytree(common, licenses / 'common', dirs_exist_ok=True)
    (output / 'runtime-files.json').write_text(json.dumps(sorted(manifest, key=lambda x: x['file']), indent=2), encoding='utf-8')
    (output / 'runtime-packages.json').write_text(json.dumps(package_manifest, indent=2), encoding='utf-8')
    total = sum(p.stat().st_size for p in (output / 'receiver').rglob('*') if p.is_file())
    print(f'Bundled {len(manifest)} native files from {len(used_packages)} packages, {total / 1024**2:.1f} MiB')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--msys', type=Path, default=Path('C:/msys64'))
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--receiver', type=Path, required=True)
    args = parser.parse_args()
    collect(args.msys.resolve(), args.output.resolve(), args.receiver.resolve())
