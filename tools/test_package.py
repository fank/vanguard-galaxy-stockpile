import pathlib
import stat
import tempfile
import unittest
import zipfile
from package import NAMES, validate


class PackageTests(unittest.TestCase):
    def test_layout_and_rejections(self):
        for change in ("valid", "missing", "empty", "symlink", "extra", "traversal"):
            with self.subTest(change=change), tempfile.TemporaryDirectory() as root:
                path = pathlib.Path(root) / "test.zip"
                with zipfile.ZipFile(path, "w") as archive:
                    for name in NAMES:
                        if change == "missing" and name == "LICENSE":
                            continue
                        info = zipfile.ZipInfo("VGStockpile/" + name)
                        if change == "symlink" and name == "LICENSE":
                            info.create_system = 3
                            info.external_attr = (stat.S_IFLNK | 0o777) << 16
                        archive.writestr(info, b"" if change == "empty" else b"synthetic")
                    if change in ("extra", "traversal"):
                        archive.writestr("VGStockpile/Assembly-CSharp.dll" if change == "extra" else "../outside", b"synthetic")
                if change == "valid":
                    validate(path)
                else:
                    with self.assertRaises(ValueError):
                        validate(path)


if __name__ == "__main__":
    unittest.main()
