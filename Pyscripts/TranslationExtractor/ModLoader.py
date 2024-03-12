import os
import os.path as OP
from lxml import etree as ET


class ModLoader:
    _default_folders = ["Defs", "Patches"]

    def __init__(self, mod_dir: str, vertuple: tuple = (1, 4)):
        """
        :param mod_dir: mod文件夹的绝对路径
        :param vertuple: RimWorld版本号，默认为1.4，使用tuple(int, int)表示
        """
        if not OP.isabs(mod_dir):
            raise ValueError("mod_dir must be an absolute path")
        self.mod_dir = mod_dir
        self.dirs = [
            f
            for f in os.listdir(mod_dir)
            if OP.isdir(f"{mod_dir}/{f}") and not (f[0] == "." or f[0] == "_")
        ]
        self.loadfolder = next(
            (f for f in os.listdir(mod_dir) if "loadfolder" in f.lower()), None
        )
        self.loadfolders = set()
        if self.loadfolder:
            self._loadfolders(vertuple=vertuple)
        else:
            self.loadfolders = self._default_folders
        print(self.loadfolders)

    def _loadfolders(self, vertuple: tuple = (1, 4)):
        tree = ET.parse(f"{self.mod_dir}/{self.loadfolder}")
        root: ET._Element = tree.getroot()
        if "loadfolder" not in root.tag.lower():
            raise ValueError(
                f"Error when parsing loadfolders: expected root tag 'loadfolder', get {root.tag} "
            )
        for node in list(root):
            name: str = node.tag.lower()
            if "v" in name:
                name = name[1:]
            if vertuple == tuple(map(int, name.split("."))):
                for folder in list(node):
                    for fold in ModLoader._default_folders:
                        if OP.exists(f"{self.mod_dir}/{folder.text}/{fold}"):
                            self.loadfolders.add(os.path.normpath(f"{self.mod_dir}/{folder.text}/{fold}"))
            
    def __iter__(self):
        for folder in self.loadfolders:
            yield folder


if __name__ == "__main__":
    ModLoader("D:\\SteamLibrary\\steamapps\\common\\RimWorld\\Mods\\Core_SK_Patch")
