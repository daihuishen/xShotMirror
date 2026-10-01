"""Collect exact MSYS2 source packages and the source tree used for this release."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
from pathlib import Path
import subprocess
import urllib.request
import shutil
import zipfile

ROOT = Path(__file__).resolve().parents[1]


def download_source(package, cache):
    name = package['source_archive']
    target = cache / name
    url = 'https://repo.msys2.org/mingw/sources/' + name
    if not target.exists():
        temporary = target.with_suffix(target.suffix + '.partial')
        with urllib.request.urlopen(urllib.request.Request(url, method='HEAD'), timeout=60) as response:
            size = int(response.headers['Content-Length'])
        if size > 128 * 1024**2:
            # Large archive responses can stall behind proxies; bounded ranges are resumable.
            chunk_size = 16 * 1024**2
            with temporary.open('wb') as output:
                for start in range(0, size, chunk_size):
                    end = min(size - 1, start + chunk_size - 1)
                    request = urllib.request.Request(url, headers={'Range': f'bytes={start}-{end}'})
                    with urllib.request.urlopen(request, timeout=120) as response:
                        if response.status != 206:
                            raise RuntimeError(f'Server did not honor range: {url}')
                        content = response.read()
                    if len(content) != end - start + 1:
                        raise RuntimeError(f'Incomplete range: {url}')
                    output.write(content)
        else:
            with urllib.request.urlopen(url, timeout=120) as response, temporary.open('wb') as output:
                shutil.copyfileobj(response, output)
        if temporary.stat().st_size != size:
            raise RuntimeError(f'Incomplete source archive: {url}')
        temporary.replace(target)
        print(f'Downloaded {name}', flush=True)
    return {'file': name, 'url': url, 'sha256': hashlib.sha256(target.read_bytes()).hexdigest()}


def tracked_files(directory):
    result = subprocess.check_output(['git', '-c', f'safe.directory={directory.as_posix()}',
                                      '-C', str(directory), 'ls-files', '--cached', '--others',
                                      '--exclude-standard', '-z'])
    return sorted(set(result.decode('utf-8').strip('\0').split('\0')))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--version', default='1.0.0')
    parser.add_argument('--publish', type=Path, default=ROOT / 'artifacts/publish')
    args = parser.parse_args()
    cache = ROOT / '.tools/source-cache'
    cache.mkdir(parents=True, exist_ok=True)
    packages = json.loads((args.publish / 'runtime-packages.json').read_text())
    unique = {p['source_archive']: p for p in packages}
    with ThreadPoolExecutor(max_workers=4) as pool:
        records = list(pool.map(lambda p: download_source(p, cache), unique.values()))
    manifest = json.dumps(records, indent=2)
    (args.publish / 'source-archives.json').write_text(manifest, encoding='utf-8')
    destination = ROOT / 'dist' / f'xShotMirror-{args.version}-sources.zip'
    destination.parent.mkdir(exist_ok=True)
    with zipfile.ZipFile(destination, 'w', compression=zipfile.ZIP_DEFLATED) as archive:
        for name in tracked_files(ROOT):
            path = ROOT / name
            if path.is_file():
                archive.write(path, 'xShotMirror/' + name)
        upstream = ROOT / 'third_party/UxPlay'
        for name in tracked_files(upstream):
            path = upstream / name
            if path.is_file() and not name.startswith(('build/', 'build-bonjour/')):
                archive.write(path, 'xShotMirror/third_party/UxPlay/' + name)
        for record in records:
            archive.write(cache / record['file'], 'dependencies/' + record['file'], compress_type=zipfile.ZIP_STORED)
        archive.writestr('dependencies/source-archives.json', manifest)
        archive.write(args.publish / 'runtime-packages.json', 'dependencies/runtime-packages.json')
    print(f'Source archive: {destination} ({destination.stat().st_size / 1024**2:.1f} MiB; {len(records)} dependency source packages)')
