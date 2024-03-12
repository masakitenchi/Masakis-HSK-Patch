import os
import os.path as OP


def BFS(root: str, format: list[str]) -> list[str]:
	"""给定根目录，递归查找所有格式存在于format内的文件

	:param root: 根目录
	:param format: 文件格式
	"""
	result = []
	for cur, dirs, files in os.walk(root):
		for file in files:
			result.append(OP.abspath(OP.join(cur, file)))
		for dir in dirs:
			result.extend(BFS(OP.abspath(OP.join(cur, dir)), format))
	return result


def listdir_abspath(path: str, format: list[str]) -> list[str]:
    return [
        os.path.abspath(os.path.join(path, f))
        for f in os.listdir(path)
        if f.split('.')[-1] in format
    ]