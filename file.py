import xml.etree.ElementTree as ET
import re
import os
import io
import tkinter as tk
from tkinter import filedialog
import argparse
import sys

dirname = os.path.split(__file__)[0]
errorfile = open(os.path.join(dirname, "error.log"), "w", encoding="utf-8")
logfile = open(os.path.join(dirname, "output.log"), "w", encoding="utf-8")


def choose_dir() -> str:
    root = tk.Tk()
    root.withdraw()

    while True:
        dir = filedialog.askdirectory(mustexist=True, initialdir=dirname, title="选择文件夹")
        if os.path.isdir(dir) and os.path.isabs(dir):
            return dir
        else:
            print("无效路径！")
            sys.exit(-1)


def expunge_newTemp(*, filepath: str, inError: bool = False) -> None:
    """清理单个文件"""
    if not os.path.isfile(filepath):
        print("Given path is not a file or abs path")
        return
    try:
        count = 0
        tree = ET.parse(filepath)
        root = tree.getroot()
        for elem in root.findall("./LanguageData"):
            # 中文不分单复数
            if "Plural" in elem.tag:
                count += 1
                root.remove(elem)
            # 可选，除非某个动物的不同生命阶段在中文中的描述不同，且并非由“幼年”之类的形容词修饰
            elif "lifeStages" in elem.tag and "0" not in elem.tag:
                count += 1
                root.remove(elem)
            # 移除机械族的性别相关标签
            elif "Mech" in elem.tag and (
                "Male" in elem.tag or "Female" in elem.tag
            ):
                count += 1
                root.remove(elem)
            # 移除所有Stuff的stuffAdjective（除非不能直接用材料名修饰以该材料制成的物品）
            elif "stuffAdjective" in elem.tag:
                count += 1
                root.remove(elem)
        if count > 0:
            logfile.write(f"removed {count} lines in {os.path.split(filepath)[1]}.\n")
        tree.write(filepath, encoding="utf-8", xml_declaration=True)
    except ET.ParseError as error:
        print("Error when parsing " + filepath + f" : {sys.exception()}")
        errorfile.write(f"Error in {os.path.split(filepath)[1]}:\n")
        with open(filepath, "r", encoding="utf-8") as f:
            lines = f.readlines()
        with open(filepath, "w", encoding="utf-8") as f:
            errorfile.write(" before:\n  " + "\n".join(lines))
            errorfile.write("\n")   
            del lines[error.position[0] - 1]
            errorfile.write(" after:\n  " + "\n".join(lines))
            errorfile.write("\n")
            f.writelines(lines)

    except:
        print("Error when parsing " + filepath + f" : {sys.exception()}")


def expunge(*, dirpath: str, inError: bool = False) -> None:
    if not os.path.isdir(dirpath) or not os.path.isabs(dirpath):
        raise Exception("Path is not a dir or absolute path")
    files = [os.path.join(dirpath, f) for f in os.listdir(dirpath) if f.endswith(".xml")]

    for file in files:
        #print(file)
        expunge_newTemp(filepath=file, inError=False)
        


def Traverse(*, path: str, In_Language: bool = False) -> None:
    if (
        all(
            os.path.isfile(os.path.join(path, entry))
            or os.path.islink(os.path.join(path, entry))
            for entry in os.listdir(path)
        )
        and In_Language
    ):
        expunge(dirpath=path)
    else:
        if path.__contains__("Language"):
            In_Language = True
        for entry in os.listdir(path):
            full_path = os.path.join(path, entry)
            if os.path.isdir(full_path):
                Traverse(path=full_path, In_Language=In_Language)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--folder", "-f", action="store", metavar="目标文件夹")
    result = parser.parse_args()
    if not result.folder:
        path = choose_dir()
    print("脚本路径：" + __file__)
    # expunge(path)
    Traverse(path=path)
