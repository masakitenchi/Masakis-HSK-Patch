from TranslationExtractor import PE
from tkinter import *
from tkinter.ttk import *
from tkinter import filedialog, messagebox
import lxml.etree as ET
from file import BFS, listdir_abspath
import os

class Patch_Extract_Tab(Frame):
    _singleton = None

    def __new__(cls, Tab: Widget):
        if Patch_Extract_Tab._singleton is None:
            Patch_Extract_Tab._singleton = super(Patch_Extract_Tab, cls).__new__(cls)
        return Patch_Extract_Tab._singleton

    def __init__(self, Tab: Widget):
        super().__init__(Tab)
        self.cur_state = "SelectDir"
        self.dirs = []
        self.DirOptions = []
        self.data_vars = []
        self.Tab = Tab
        self.ext_dir = StringVar(self.Tab, name="ext_dir1")
        self.ext_dir.trace_add('write', self.update)
        #用于让Entry不那么频繁触发ext_dir的trace的变量
        self.ext_dir_wrap = StringVar(self.Tab, name="ext_dir_wrap")
        self.Title = Frame(Tab, height=100, style='white.TFrame')
        self.Title.grid(row=0, sticky='nswe', columnspan=2)
        self.Title.grid_rowconfigure(0, weight=1)
        self.Title.grid_columnconfigure(0, weight=1)
        self.Title.grid_columnconfigure(1, weight=9)
        self.Draw_Title(self.Title)
        self.Tab.grid_columnconfigure(0, weight=1)
        self.Tab.grid_rowconfigure(0, minsize=50)
        self.Main_Rect = Frame(Tab, style='Green.TFrame')
        self.Main_Rect.grid(row=1, column=0, sticky='nswe')
        self.Main_Rect.grid_rowconfigure(0, weight=1)
        self.Main_Rect.grid_columnconfigure(0, weight=1)
        self.Draw_Main(self.Main_Rect)
        self.Frame_Config = Frame(Tab, style='yellow.TFrame', width=100)
        self.Frame_Config.grid(row=1, column=1, sticky='nswe')
        self.Draw_Config(self.Frame_Config)
        self.Bottom_Buttons = Frame(Tab, width=Tab.winfo_reqwidth(), height=20, style='Blue.TFrame')
        self.Bottom_Buttons.grid(row=2, sticky= 'nswe', columnspan=2)
        self.Draw_Bottom(self.Bottom_Buttons)
        self.Tab.rowconfigure(0, minsize=40, pad=10)
        self.Tab.rowconfigure(1, weight=1)
        self.Tab.rowconfigure(2, minsize=40, pad=10)
        self.Tab.columnconfigure(1, weight=1, minsize=150)
        self.Tab.columnconfigure(0, weight=9)
        """ self.entry = Entry(Tab)
        def execute(event):
            command = self.entry.get()
            print(eval(command))
        self.entry.bind("<Return>", execute)
        self.entry.grid(row=2, column=1, sticky='we') """
        self.Title.bind_all("<ButtonPress-1>", self.check_direntry)
        self.update()
    def update(self, *args):
        #print(args)
        if not self.ext_dir.get(): return
        max_width = 0
        self.ext_dir_wrap.set(self.ext_dir.get())
        #self.canvas.grid_forget()
        self.dirs.clear()
        self.data_vars.clear()
        for button in self.DirOptions:
            button.destroy()
        self.canvas.delete('all')
        self.Checkboxes = Frame(self.canvas)
        row = 1
        self.dirs.extend([f"{self.ext_dir.get()}/{f}" for f in os.listdir(self.ext_dir.get()) if os.path.isdir(f"{self.ext_dir.get()}/{f}") and not (f[0] == "." or f[0] == "_")])
        self.data_vars.extend([IntVar(self.Tab, 0) for i in range(len(self.dirs))])
        for i in range(len(self.dirs)):
            button = Checkbutton(self.Checkboxes, text=self.dirs[i], variable=self.data_vars[i], name=f"dir{i}", command=self.check_checkbox)
            button.grid(row=i+row, column=0, sticky='we')
            button.update_idletasks()
            max_width = max(max_width, button.winfo_width())
            #print(button.winfo_reqwidth())
            self.DirOptions.append(button)
            row += 1
        # update must be placed before actually drawing (e.g. with grid or pack) the widgets
        # I'm still confused by how this actually works
        #就算直接调整Main_Rect的大小也无济于事，在update_idletasks之后甚至还因为propagate变宽了
        #感觉要在点击按钮之后直接在self.Tab里调整才行……
        self.canvas.update_idletasks()
        self.canvas.create_window((0, 0), window=self.Checkboxes, anchor="nw", tags="MainFrame")
        self.canvas.configure(scrollregion=self.canvas.bbox("all"))
        self.canvas.grid(sticky='nswe')
        print(self.canvas.bbox('all')[3])
        print(self.Main_Rect.winfo_height())
        if self.canvas.bbox('all')[2] > self.Main_Rect.winfo_width() + 20:
            self.scrbrX.grid(row=1, column=0, sticky='we')
        else:
            self.scrbrX.grid_forget()
        if self.canvas.bbox('all')[3] > self.Main_Rect.winfo_height():
            self.canvas.bind_all("<MouseWheel>", self.on_mousewheel)
            self.scrbrY.grid(row=0, column=1, sticky='ns')
        else:
            self.canvas.unbind_all("<MouseWheel>")
            self.scrbrY.grid_forget()
        """ self.Draw_Title(self.Title)
        self.Draw_Main(self.Main_Rect)
        self.Draw_Config(self.Frame_Config)
        self.Draw_Bottom(self.Bottom_Buttons) """
        super().update()


    def Draw_Title(self, outRect: Widget, **kwargs):
        Button(
            self.Title,
            text="Choose directory",
            command=lambda: (
                self.ext_dir.set(
                    filedialog.askdirectory(mustexist=True, title="选择目标文件夹")
                )
            )
        ).grid(row=0, column=0, sticky='we')
        self.Entry = Entry(
            self.Title,
            textvariable=self.ext_dir_wrap
        )
        self.Entry.grid(row=0, column=1, sticky='we')
        #还是这个方便
        self.Entry.bind("<Return>", self.update_dir)
        self.Entry.bind("<FocusOut>", self.update_dir)
    def Draw_Main(self, outRect: Widget, **kwargs):
        self.canvas = Canvas(self.Main_Rect)
        self.scrbrY = Scrollbar(self.Main_Rect, orient=VERTICAL, command=self.canvas.yview)
        self.scrbrX = Scrollbar(self.Main_Rect, orient=HORIZONTAL, command=self.canvas.xview)
        self.canvas.configure(xscrollcommand=self.scrbrX.set, yscrollcommand=self.scrbrY.set, scrollregion=self.canvas.bbox("all"))
    def Draw_Config(self, outRect: Widget, **kwargs):
        self.configBoxes = []
        self.recursive = BooleanVar(self.Tab, True)
        self.append = BooleanVar(self.Tab, False)
        self.split = BooleanVar(self.Tab, False)
        self.split.trace_add(
            "write",
            lambda a, b, c: (
                self.append_option.config(state="normal")
                if self.split.get()
                else (self.append_option.config(state="disabled"), self.append.set(False))
            ),
        )
        self.Defs = BooleanVar(self.Tab, False)
        self.Patches = BooleanVar(self.Tab, True)
        outRect.rowconfigure(0, weight=1)
        outRect.rowconfigure(1, weight=9)
        outRect.columnconfigure(0, weight=1)
        self.TargetGrid = Labelframe(outRect, text="Configs")
        self.TargetGrid.rowconfigure(0, weight=1)
        self.TargetGrid.columnconfigure(0, weight=1)
        self.TargetGrid.grid(row=0, column=0, sticky='nswe')
        configRect = Frame(self.TargetGrid)
        configRect.columnconfigure(0, weight=1)
        configRect.grid(row=0, column=0, sticky='nswe')
        self.recursive_option = Checkbutton(configRect, text="Recursive", variable=self.recursive)
        self.recursive_option.grid(row=0, column=0, sticky='w')
        Label(configRect, text="递归查找").grid(row=0, column=1, sticky='w')
        self.split_option = Checkbutton(configRect, text="Split", variable=self.split)
        self.split_option.grid(row=1, column=0, sticky='w')
        Label(configRect, text="按文件分割").grid(row=1, column=1, sticky='w')
        self.append_option = Checkbutton(configRect, text="Append", variable=self.append, state="disabled")
        self.append_option.grid(row=2,column=0, sticky='w')
        Label(configRect, text="追加文件夹名到文件名后").grid(row=2, column=1, sticky='w')
        self.Targets = [
            Checkbutton(configRect, text="Def", variable=self.Defs),
            Label(configRect, text="提取Defs"),
            Checkbutton(configRect, text="Patch", variable=self.Patches),
            Label(configRect, text="提取Patch"),
        ]
        self.Targets[0].grid(row=3, column=0, sticky='w')
        self.Targets[1].grid(row=3, column=1, sticky='w')
        self.Targets[2].grid(row=4, column=0, sticky='w')
        self.Targets[3].grid(row=4, column=1, sticky='w')
        Separator(configRect, orient=HORIZONTAL).grid(row=5, column=0, sticky='we', columnspan=2)
        self.informationRect = Frame(outRect, width=outRect.winfo_width())
        self.informationRect.rowconfigure(0, weight=1)
        self.informationRect.columnconfigure(0, weight=1)
        self.informationRect.grid(row=1, column=0, sticky='nswe')
        self.infoText = Text(self.informationRect, wrap=NONE, state='normal', width=self.informationRect.winfo_width())
        self.infoTextscrbrX = Scrollbar(self.informationRect, orient=HORIZONTAL, command=self.infoText.xview)
        self.infoTextscrbrY = Scrollbar(self.informationRect, orient=VERTICAL, command=self.infoText.yview)
        self.infoText.configure(xscrollcommand=self.infoTextscrbrX.set, yscrollcommand=self.infoTextscrbrY.set)
        self.infoText.insert(1.0, "This is a testSIDFOIDFJOIDJSFOISJDOFJSDOFONASODNAOSIDNAOSNONDSDSFSDFASDASD")
        self.infoText.grid(row=0, column=0, sticky='wnse', padx=2, pady=2)
        self.infoTextscrbrX.grid(row=1, column=0, sticky='we')
        self.infoTextscrbrY.grid(row=0, column=1, sticky='ns')
    def Draw_Bottom(self, outRect: Widget, **kwargs):
        self.ExportButton = Button(outRect, text="Output Selected", command=self.do_work, name="output", state='disabled')
        self.ExportButton.place(relx=0.5, rely=0.5, anchor=CENTER)
        pass
    def do_work(self):
        self.Tab.columnconfigure(0, weight=6)
        self.Tab.columnconfigure(1, weight=4, minsize=300)
        dirs = (dirname for i, dirname in enumerate(self.dirs) if self.data_vars[i].get())
        targets = set()
        if self.Defs.get(): targets.add('Def')
        if self.Patches.get(): targets.add('Patch')
        recursive = self.recursive.get()
        split = self.split.get()
        append = self.append.get()
        Total_dir = filedialog.askdirectory(mustexist=True, title="保存到……")
        for dir in dirs:
            mod_name = os.path.basename(dir)
            output_dir = f"{Total_dir}/{mod_name}/Languages/ChineseSimplified/DefInjected"
            os.makedirs(output_dir, exist_ok=True)
            files = []
            for file in (BFS(dir, ["xml"]) if recursive else listdir_abspath(dir, ["xml"])):
                try:
                    root = ET.parse(file).getroot()
                    if not(root.tag == "Defs" or root.tag == "Patch"):
                        continue
                    files.append(file)
                except:
                    continue
            #print(f'result: {files}')
            if split:
                for file in files:
                    KVpair = PE.extract([file], targets)
                    for defType, KVpair in KVpair.items():
                        elemroot: ET._Element = ET.Element("LanguageData")
                        elemroot.addprevious(
                            ET.Comment("This file was generated by Patch_Extract.py")
                        )
                        tree: ET._ElementTree = ET.ElementTree(elemroot)
                        basename = os.path.basename(os.path.dirname(file))
                        file_name = (
                            os.path.basename(file).split(".")[0]
                            + (f"_{basename}" if append else "")
                            + ".xml"
                        )
                        #os.makedirs(os.path.join(output_dir, defType), exist_ok=True)
                        os.makedirs(os.path.join(output_dir, defType, 'Defs'), exist_ok=True)
                        os.makedirs(os.path.join(output_dir, defType, 'Patches'), exist_ok=True)
                        for key, value in KVpair.items():
                            if value == "" or value is None: continue
                            defName = ET.SubElement(elemroot, key)
                            defName.addprevious(ET.Comment("EN: " + value)) # Add original text as comment
                            defName.text = value
                        #print(f"{output_dir}/{defType}/{file_name}")
                        
                        if 'Defs' in file:
                            output_path = f"{output_dir}/{defType}/Defs/{file_name}"
                        elif 'Patches' in file:
                            output_path = f"{output_dir}/{defType}/Patches/{file_name}"
                        else:
                            output_path = f"{output_dir}/{defType}/{file_name}"
                        self.infoText.insert(END, f"Writing to {output_path}\n")
                        tree.write(
                            output_path,
                            pretty_print=True,
                            xml_declaration=True,
                            encoding="utf-8"
                        )
            else:
                results = PE.extract(files, targets)
                for defType, KVpair in results.items():
                    os.makedirs(os.path.join(output_dir, defType), exist_ok=True)
                    elemroot: ET._Element = ET.Element("LanguageData")
                    elemroot.addprevious(
                        ET.Comment("This file was generated by Patch_Extract.py")
                    )
                    etree: ET._ElementTree = ET.ElementTree(elemroot)
                    for key, value in KVpair.items():
                        if value == "" or value is None: continue
                        defName = ET.SubElement(elemroot, key)
                        defName.addprevious(ET.Comment("EN: " + value))
                        defName.text = value
                    output_path = f"{output_dir}/{defType}/Extracted.xml"
                    self.infoText.insert(END, f"Writing to {output_path}\n")
                    etree.write(output_path, pretty_print=True, xml_declaration=True, encoding='utf-8')
        messagebox.showinfo("Done", "Extraction complete!")
        
    def check_direntry(self, *args):
        if self.winfo_containing(self.winfo_pointerx(), self.winfo_pointery()) != self.Entry:
            self.Tab.focus_set()
    def check_checkbox(self, *args):
        print(args)
        if any(f.get() for f in self.data_vars):
            self.ExportButton.config(state='normal')
        else:
            self.ExportButton.config(state='disabled')
    def update_dir(self, *args):
        print(args)
        self.ext_dir.set(self.ext_dir_wrap.get())
    def on_mousewheel(self, event):
        #print(self.canvas.xview(), self.canvas.yview())
        #if self.winfo_containing(self.winfo_pointerx(), self.winfo_pointery()) == self.canvas:
        self.canvas.yview_scroll(-1 * (event.delta // 120), "units")


class MainWindow(Tk):
    def __init__(self):
        super().__init__()
        Style().configure("Red.TFrame", background="red", theme='vista')
        Style().configure("Blue.TFrame", background="blue", theme='vista')
        Style().configure("Green.TFrame", background="green", theme='vista')
        Style().configure('yellow.TFrame', background='yellow', theme='vista')
        Style().configure('white.TFrame', background='white', theme='vista')
        self.title("Pyscripts")
        self.geometry("800x600")
        self.resizable(False,False)
        self.notebook = Notebook(self, width=800, height=600)
        print(self.notebook.winfo_geometry())
        """ self.Patch_Extract = Frame(self.notebook)
        self.Patch_Extract.grid() """
        self.Translation_Clean = Frame(self.notebook)
        self.Test = Frame(self.notebook, style='Red.TFrame', width=800, height=600)
        self.Test.grid()
        self.l =Label(self.notebook, text="Starting...")
        self.l.grid()
        self.l.bind('<Enter>', lambda e: self.l.configure(text='Moved mouse inside'))
        self.l.bind('<Leave>', lambda e: self.l.configure(text='Moved mouse outside'))
        self.l.bind('<ButtonPress-1>', lambda e: self.l.configure(text='Clicked left mouse button'))
        self.l.bind('<3>', lambda e: self.l.configure(text='Clicked right mouse button'))
        self.l.bind('<Double-1>', lambda e: self.l.configure(text='Double clicked'))
        self.l.bind('<B3-Motion>', lambda e: self.l.configure(text='right button drag to %d,%d' % (e.x, e.y)))
        self.notebook.add(self.Test, text="Patch Extractor")
        #self.notebook.add(self.Translation_Clean, text="Translation Cleaner")
        self.notebook.add(self.l, text="Test")
        #self.notebook.hide(self.notebook.index(self.Patch_Extract))
        self.notebook.pack()
        #Patch_Extract_Tab(self.Patch_Extract)
        self.Test_Tab = Patch_Extract_Tab(self.Test)


if __name__ == "__main__":
    app = MainWindow()
    Style(app).theme_use('vista')
    app.mainloop()
