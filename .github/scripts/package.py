import argparse
import gzip
import hashlib
import io
import json
import os
import re
import sys
import tarfile
import zipfile

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FIXED_TIME = (2020, 1, 1, 0, 0, 0)
GUID = re.compile(r"^guid:\s*([0-9a-f]{32})\s*$", re.MULTILINE)


def is_packaged(relative_path):
    return not any(part.startswith(".") or part.endswith("~") for part in relative_path.split("/"))


def collect(output):
    assets, errors = [], []
    output = os.path.abspath(output)
    for directory, folders, files in os.walk(ROOT):
        folders.sort()
        relative_dir = os.path.relpath(directory, ROOT).replace(os.sep, "/")
        relative_dir = "" if relative_dir == "." else relative_dir
        folders[:] = [
            f for f in folders
            if is_packaged(f"{relative_dir}/{f}".lstrip("/")) and os.path.abspath(os.path.join(directory, f)) != output
        ]
        names = set(files)
        for name in sorted(files):
            path = f"{relative_dir}/{name}".lstrip("/")
            if not is_packaged(path):
                continue
            if name.endswith(".meta"):
                target = name[:-5]
                if target not in names and target not in folders:
                    errors.append(f"orphan meta: {path}")
                continue
            if name + ".meta" not in names:
                errors.append(f"missing meta: {path}")
            assets.append(path)
        for folder in folders:
            path = f"{relative_dir}/{folder}".lstrip("/")
            if folder + ".meta" not in names:
                errors.append(f"missing meta: {path}/")
            assets.append(path + "/")
    if errors:
        sys.exit("\n".join(errors))
    return sorted(assets)


def read(path):
    with open(os.path.join(ROOT, path), "rb") as file:
        return file.read()


def guid_of(asset):
    match = GUID.search(read(asset.rstrip("/") + ".meta").decode("utf-8"))
    if not match:
        sys.exit(f"no guid in {asset.rstrip('/')}.meta")
    return match.group(1)


def manifest_bytes(manifest):
    return (json.dumps(manifest, indent=2, ensure_ascii=False) + "\n").encode("utf-8")


def build_zip(path, assets, manifest):
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for asset in assets:
            entries = [asset.rstrip("/") + ".meta"]
            if not asset.endswith("/"):
                entries.insert(0, asset)
            for entry in entries:
                data = manifest_bytes(manifest) if entry == "package.json" else read(entry)
                info = zipfile.ZipInfo(entry, FIXED_TIME)
                info.compress_type = zipfile.ZIP_DEFLATED
                info.external_attr = 0o644 << 16
                archive.writestr(info, data)


def add_tar_file(archive, name, data):
    info = tarfile.TarInfo(name)
    info.size = len(data)
    info.mode = 0o777
    info.mtime = 1577836800
    archive.addfile(info, io.BytesIO(data))


def build_unitypackage(path, assets, manifest, install_root):
    tar = io.BytesIO()
    with tarfile.open(fileobj=tar, mode="w", format=tarfile.GNU_FORMAT) as archive:
        for asset in assets:
            guid = guid_of(asset)
            folder = tarfile.TarInfo(guid)
            folder.type = tarfile.DIRTYPE
            folder.mode = 0o777
            folder.mtime = 1577836800
            archive.addfile(folder)
            if not asset.endswith("/"):
                data = manifest_bytes(manifest) if asset == "package.json" else read(asset)
                add_tar_file(archive, f"{guid}/asset", data)
            add_tar_file(archive, f"{guid}/asset.meta", read(asset.rstrip("/") + ".meta"))
            add_tar_file(archive, f"{guid}/pathname", f"{install_root}/{asset.rstrip('/')}".encode("utf-8"))
    with open(path, "wb") as raw, gzip.GzipFile(filename="archtemp.tar", mode="wb", fileobj=raw, mtime=0) as compressed:
        compressed.write(tar.getvalue())


def sha256(path):
    digest = hashlib.sha256()
    with open(path, "rb") as file:
        for block in iter(lambda: file.read(1 << 16), b""):
            digest.update(block)
    return digest.hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", required=True)
    parser.add_argument("--tag")
    parser.add_argument("--out", default="dist")
    args = parser.parse_args()

    manifest = json.loads(read("package.json"))
    name, version = manifest["name"], manifest["version"]
    tag = args.tag or f"v{version}"
    if tag != f"v{version}":
        sys.exit(f"tag {tag} does not match package.json version {version}")

    os.makedirs(args.out, exist_ok=True)
    stem = f"{name}-{version}"
    zip_path = os.path.join(args.out, f"{stem}.zip")
    unitypackage_path = os.path.join(args.out, f"{stem}.unitypackage")

    manifest["url"] = f"https://github.com/{args.repo}/releases/download/{tag}/{stem}.zip"
    assets = collect(args.out)
    build_zip(zip_path, assets, manifest)
    build_unitypackage(unitypackage_path, assets, manifest, f"Packages/{name}")

    listing_entry = dict(manifest, zipSHA256=sha256(zip_path))
    with open(os.path.join(args.out, "package.json"), "wb") as file:
        file.write(manifest_bytes(listing_entry))

    print(f"{len(assets)} assets")
    for path in (zip_path, unitypackage_path, os.path.join(args.out, "package.json")):
        print(f"{os.path.getsize(path):>9} B  {path}")
    print(f"zipSHA256 {listing_entry['zipSHA256']}")


if __name__ == "__main__":
    main()
