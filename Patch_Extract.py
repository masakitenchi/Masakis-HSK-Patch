import os
import os.path
import lxml.etree as ET
import regex as re
import json as JSON

# Find all patches that target defs with defName
xpath_regex = re.compile(r'Defs\/(?<defType>.*?Def)\[(?<defNames>.*)\]\/(?<field>label|description)')

defNames_regex = re.compile(r'defName\W*?=\W*?"(?<defName>.*?)"')

PatchOperations = ['PatchOperationAdd', 'PatchOperationReplace', 'PatchOperationSequence', 'PatchOperationConditional']
errors = []

pairs : dict[str, dict[str,str]]= dict()

def get_files_abspath(path: str) -> list[str]:
	return [os.path.abspath(f) for f in os.listdir(path) if f.endswith('.xml')]


def extract(list_paths: list[str]) -> None:
	#os.makedirs('extracted', exist_ok=True)
	for file in list_paths:
		if not os.path.isabs(file) or not os.path.isfile(file): 
			errors.append(f'path {file} does not target a file or is not absolute')
			continue
		tree = ET.parse(file)
		root : ET._Element = tree.getroot()
		nodes = root.xpath('//*/xpath[contains(text(),"label") or contains(text(), "description")]/..')
		for node in nodes:
			xpath = node.find('./xpath')
			defType, defName, field = re.match(xpath_regex, xpath.text).groups()
			value = node.find(f'./value/{field}').text
			if defType not in pairs:
				pairs[defType] = dict()
			defNames = re.findall(defNames_regex, defName)
			#print(defNames)
			for defName in defNames:
				key = f'{defName}.{field}'
				pairs[defType][key] = value
			#print(pairs)
				

			#os.makedirs(f'extracted/{defType}', exist_ok=True)
			
			#print(ET.tostring(node, pretty_print=True))

def BFS(root: str) -> list[str]:
	result = list()
	for cur, leafs, files in os.walk(root):
		for file in files:
			if file.endswith('.xml'):
				result.append(os.path.abspath(os.path.join(cur, file)))
		for leaf in leafs:
			result.extend(BFS(leaf))
	return result


if __name__ == '__main__':
	# Get all files in the directory recursively
	list_files = BFS('.')
	extract(list_files)
	print(pairs)
	os.makedirs('extracted', exist_ok=True)
	for defType, results in pairs.items():
		os.makedirs(f'extracted/{defType}', exist_ok=True)
		root : ET._Element = ET.Element('LanguageData')
		root.addprevious(ET.Comment('This file was generated by Patch_Extract.py'))
		tree : ET._ElementTree = ET.ElementTree(root)
		for key, value in results.items():
			# Create the defName node
			defName = ET.SubElement(root, key)
			defName.text = value
		tree.write(f'extracted/{defType}/ExtractedPatch.xml', pretty_print=True, xml_declaration=True, encoding='utf-8')
	if len(errors) > 0:
		for e in errors:
			print(e)
		exit(1)
	exit(0)
