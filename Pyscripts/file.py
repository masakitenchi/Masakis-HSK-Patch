import tkinter as tk
from tkinter import filedialog, messagebox
import os


def choose_dir(root: tk, title:str) -> str:
	root.withdraw()
	while True:
		dir = filedialog.askdirectory(mustexist=True, title=title)
		if os.path.isdir(dir) and os.path.isabs(dir):
			return dir
		else:
			messagebox.Message(message="无效路径！", title="错误", icon="error").show()
			

def choose_dirs(root: tk, title:str) -> list[str]:
	root.withdraw()
	dirs = []
	while True:
		_dir = filedialog.askdirectory(mustexist=True, title=title)
		if os.path.isdir(_dir) and os.path.isabs(_dir):
			dirs.append(_dir)
		else:
			messagebox.Message(message="无效路径！", title="错误", icon="error").show()
