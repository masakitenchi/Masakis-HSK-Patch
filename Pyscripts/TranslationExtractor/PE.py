import os
import os.path
import lxml.etree as ET
import regex as re
import argparse

# Find all patches that target defs with defName
xpath_regex = re.compile(
    r"Defs\/(?<defType>.*?Def)\[(?<defNames>.*)\]\/(?<field>label|description)"
)

defNames_regex = re.compile(r'defName\W*?=\W*?"(?<defName>.*?)"')

PatchOperations = [
    "PatchOperationAdd",
    "PatchOperationReplace",
    "PatchOperationSequence",
    "PatchOperationConditional",
]
errors = []

Defs_xpath = "//*[preceding-sibling::defName or following-sibling::defName][self::label or self::description]/.."

Patch_xpath = '//xpath[text()="label" or text()="description"]/..'

#lxml seems doesn't support local-name()
Anomaly_xpath = '//*[@Class="PatchOperationAdd"]/xpath[text()="Defs"]/../value/*[not(@Abstract) or (@Abstract!="True" and @Abstract!="true")]'



def extract(list_paths: list[str]) -> dict[str, dict[str, str]]:
    """总提取函数

    :param list_paths: 所有文件的绝对路径列表
    :return: dict[defType: str, dict[defName: str, field: str]] 按defType分类的提取结果
    """
    pairs: dict[str, dict[str, str]] = dict()
    # os.makedirs('extracted', exist_ok=True)
    for file in list_paths:
        try:
            print(f"parsing {file}")
            count = 0
            if not os.path.isabs(file) or not os.path.isfile(file):
                errors.append(f"path {file} does not target a file or is not absolute")
                raise Exception(f"path {file} does not target a file or is not absolute")
            tree = ET.parse(file)
            root: ET._Element = tree.getroot()
            if root.tag == "Defs":
                # only looks for non-virtual defs
                nodes = root.xpath(Defs_xpath)
                if len(nodes) > 0: print(f"found {len(nodes)} defs in {file}")
                for node in nodes:
                    defType = node.tag
                    if defType not in pairs:
                        pairs[defType] = dict()
                    key = node.find("./defName").text
                    pairs[defType][f"{key}.label"] = (
                        node.find("./label").text
                        if node.find("./label") is not None
                        else None
                    )
                    pairs[defType][f"{key}.description"] = (
                        node.find("./description").text
                        if node.find("./description") is not None
                        else None
                    )
            elif root.tag == 'Patch':
                # looks for all patches that targeting label or description
                nodes = root.xpath(Patch_xpath)
                if len(nodes) > 0: print(f"found {len(nodes)} patches in {file}")
                for node in nodes:
                    xpath = node.find("./xpath")
                    match = re.match(xpath_regex, xpath.text)
                    if match:
                        defType, defName, field = match.groups()
                    else:
                        continue
                    value = node.find(f"./value/{field}").text
                    if defType not in pairs:
                        pairs[defType] = dict()
                    defNames = re.findall(defNames_regex, defName)
                    # print(defNames)
                    for defName in defNames:
                        key = f"{defName}.{field}"
                        pairs[defType][key] = value
                anomaly_nodes = root.xpath(Anomaly_xpath)
                if len(anomaly_nodes) > 0: print(f"found {len(anomaly_nodes)} anomaly_nodes in {file}")
                for node in anomaly_nodes:
                    defName = node.find("./defName").text
                    label = node.find("./label").text if node.find("./label") is not None else ""
                    description = node.find("./description").text if node.find("./description") is not None else ""
                    defType = node.tag
                    if defType not in pairs:
                        pairs[defType] = dict()
                    pairs[defType][f"{defName}.label"] = label
                    pairs[defType][f"{defName}.description"] = description
            
        except Exception as e:
            if node is not None:
                print(f"Error when parsing {file}, {node.tag} message: {e}")
            else:
             print(f"Error when parsing {file}, message: {e}")
            continue
    return pairs


def BFS(root: str) -> list[str]:
    result = list()
    for cur, leafs, files in os.walk(root):
        for file in files:
            if file.endswith(".xml"):
                result.append(os.path.abspath(os.path.join(cur, file)))
        for leaf in leafs:
            result.extend(BFS(leaf))
    return result


def main(dirpath: str, recursive: bool = False) -> dict[str, ET._ElementTree]:
    if not os.path.isdir(dirpath) or not os.path.isabs(dirpath):
        raise Exception("Path is not a dir or absolute path")
    trees = dict()
    if recursive:
        list_files = BFS(dirpath)
    else:
        list_files = [os.path.abspath(f) for f in os.listdir(dirpath) if f.endswith(".xml")]
    # print(list_files)
    pairs = extract(list_files)
    # print(pairs)
    # os.makedirs('extracted', exist_ok=True)
    for defType, results in pairs.items():
        # os.makedirs(f'extracted/{defType}', exist_ok=True)
        root: ET._Element = ET.Element("LanguageData")
        root.addprevious(ET.Comment("This file was generated by Patch_Extract.py"))
        tree: ET._ElementTree = ET.ElementTree(root)
        for key, value in results.items():
            defName = ET.SubElement(root, key)
            defName.text = value
        trees[defType] = tree
    return trees


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--folder", "-f", action="store", metavar="目标文件夹")
    result = parser.parse_args()
    if not result.folder:
        path = os.path.abspath(".")
    else:
        path = os.path.abspath(result.folder)
    list_files = BFS(path)
    pairs = extract(list_files)
    print(pairs)
    os.makedirs("extracted", exist_ok=True)
    for defType, results in pairs.items():
        os.makedirs(f"extracted/{defType}", exist_ok=True)
        root: ET._Element = ET.Element("LanguageData")
        root.addprevious(ET.Comment("This file was generated by Patch_Extract.py"))
        tree: ET._ElementTree = ET.ElementTree(root)
        for key, value in results.items():
            defName = ET.SubElement(root, key)
            defName.text = value
        tree.write(
            f"extracted/{defType}/ExtractedPatch.xml",
            pretty_print=True,
            xml_declaration=True,
            encoding="utf-8",
        )
    if len(errors) > 0:
        for e in errors:
            print(e)
        exit(1)
    exit(0)
