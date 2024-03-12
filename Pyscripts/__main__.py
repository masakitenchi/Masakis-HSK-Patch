from TranslationExtractor import PE
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
    result = PE.main(ext_dir.get(), recursive_var.get())
    Label(root, text="Extraction complete!").pack()
    output_dir = filedialog.askdirectory(
        mustexist=True, title="保存到……"
    )
    for defType, tree in result.items():
        root: ET._Element = tree.getroot()
        os.makedirs(f"{output_dir}/{defType}", exist_ok=True)
        base_filename = f"{output_dir}/{defType}/Extracted"
        ext = ".xml"
        for node in list(root):
            if node.text == "" or node.text is None:
                root.remove(node)
        if os.path.exists(f"{base_filename}{ext}"):
            current = ET.parse(f"{base_filename}{ext}").getroot()
            for child in list(root):
                current.append(child)
            current.write(
                f"{base_filename}{ext}",
                pretty_print=True,
                xml_declaration=True,
                encoding="utf-8",
            )
        else:
            tree.write(
                f"{base_filename}{ext}",
                pretty_print=True,
                xml_declaration=True,
                encoding="utf-8",
            )


if __name__ == "__main__":
    root = Tk()
    root.title("Pyscripts")
    root.geometry("800x600")
    root.resizable(False, False)
    notebook = Notebook(root, width=800, height=600)
    Patch_Extract = Frame(notebook)
    Translation_Clean = Frame(notebook)
    notebook.add(Patch_Extract, text="Patch Extractor")
    # notebook.add(Translation_Clean, text="Translation Cleaner")
    notebook.pack()
    ext_dir = StringVar(root, value="undefined", name="ext_dir")
    directory_label_text = StringVar(root, value=f"The directory of the patch is: {ext_dir.get()}")
    directory_label = Label(
        Patch_Extract, textvariable=directory_label_text, wraplength=800
    )
    directory_label.pack()

    Button(
        Patch_Extract,
        text="Choose directory",
        command=lambda: (
            ext_dir.set(filedialog.askdirectory(mustexist=True, title="选择目标文件夹")),
            directory_label_text.set(f"The directory of patches is: {ext_dir.get()}")
            ),
    ).pack()
    recursive_var = BooleanVar(Patch_Extract, True)
    Checkbutton(Patch_Extract, text="Recursive", variable=recursive_var).pack()
    Button(Patch_Extract, text="Extract!", command=extract).pack()
    root.mainloop()
