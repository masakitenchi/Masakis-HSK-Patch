import TranslationExtractor.PE as PE
from tkinter import *
from tkinter.ttk import *
from tkinter import filedialog
import lxml.etree as ET
import os

ext_dir = ""
root = Tk()

def select_folder():
	global ext_dir
	ext_dir = filedialog.askdirectory(mustexist=True, title="Choose the directory")

def extract(flag: bool = False):
	global ext_dir
	Label(root, text=f"Extracting from {ext_dir}...").pack()
	result = PE.main(ext_dir, flag)
	Label(root, text="Extraction complete!").pack()
	output_dir = filedialog.askdirectory(mustexist=True, title="Choose the directory to save the extracted files")
	for defType, tree in result.items():
		os.makedirs(f"{output_dir}/{defType}", exist_ok=True)
		tree.write(f"{output_dir}/{defType}/Extracted.xml", pretty_print=True, xml_declaration=True, encoding='utf-8')
		


if __name__ == '__main__':
	root.title("Hello, world!")
	root.geometry("1024x768")
	root.resizable(True, True)
	recursive = False
	label = Label(root, text="Hello, world!")
	label.pack()
	ext_dir = filedialog.askdirectory(mustexist=True, title="Choose the directory of the patch")
	Separator(root, orient="horizontal").pack()
	Label(root, text=f"The directory of the patch is: {ext_dir}").pack()
	Frame(root, height=10,width=20).pack()
	Button(root, text="Choose another directory", command=select_folder).pack()
	Checkbutton(root, text="Recursive", onvalue=1, offvalue=0, variable=recursive).pack()
	Extract = Button(root, text="Extract!", command=extract)
	Extract.pack()
	root.mainloop()