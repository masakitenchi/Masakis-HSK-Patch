import TranslationExtractor.PE as PE
from tkinter import *
from tkinter.ttk import *
from tkinter import filedialog
import lxml.etree as ET
import os

def select_folder():
    global ext_dir
    ext_dir = filedialog.askdirectory(mustexist=True, title="Choose the directory")
    if ext_dir:
        directory_label.config(text=f"The directory of the patch is: {ext_dir}")

def extract():
    global ext_dir
    Label(root, text=f"Extracting from {ext_dir}...").pack()
    result = PE.main(ext_dir, recursive_var.get())
    Label(root, text="Extraction complete!").pack()
    output_dir = filedialog.askdirectory(mustexist=True, title="Choose the directory to save the extracted files")
    for defType, tree in result.items():
        os.makedirs(f"{output_dir}/{defType}", exist_ok=True)
        base_filename = f"{output_dir}/{defType}/Extracted"
        ext = ".xml"
        if os.path.exists(f"{base_filename}{ext}"):
            current = ET.parse(f"{base_filename}{ext}").getroot()
            for child in list(tree.getroot()):
                current.append(child)
            current.write(f"{base_filename}{ext}", pretty_print=True, xml_declaration=True, encoding='utf-8')
        else:
            tree.write(f"{base_filename}{ext}", pretty_print=True, xml_declaration=True, encoding='utf-8')

if __name__ == '__main__':
    root = Tk()
    root.title("Hello, world!")
    root.geometry("1024x768")
    
    ext_dir = filedialog.askdirectory(mustexist=True, title="Choose the directory of the patch")
    directory_label = Label(root, text=f"The directory of the patch is: {ext_dir}")
    directory_label.pack()
    
    Button(root, text="Choose another directory", command=select_folder).pack()
    recursive_var = BooleanVar(root, True)
    Checkbutton(root, text="Recursive", variable=recursive_var).pack()
    Button(root, text="Test", command=lambda: print(recursive_var.get())).pack()
    Button(root, text="Extract!", command=extract).pack()

    root.mainloop()