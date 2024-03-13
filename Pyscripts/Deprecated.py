def Test_Tab(Test: Frame):
    def on_mousewheel(event):
        canvas.yview_scroll(-1 * (event.delta // 120), "units")
    def update_dirs_and_vars(*args):
        if not ext_dir.get(): return
        row = 1
        for button in buttons:
            button.destroy()
        dirs.clear()
        dirs.extend([f"{ext_dir.get()}/{f}" for f in os.listdir(ext_dir.get()) if os.path.isdir(f"{ext_dir.get()}/{f}") and not (f[0] == "." or f[0] == "_")])
        data_vars.clear()
        data_vars.extend([IntVar(Test, 0) for i in range(len(dirs))])
        for i in range(len(dirs)):
            button = Checkbutton(Checkboxes, text=dirs[i], variable=data_vars[i], name=f"dir{i}")
            button.grid(row=i+row, column=0, sticky='we')
            Checkboxes.grid_rowconfigure(0, weight=1)
            buttons.append(button)
            row += 1
        #Frame_Checkboxes.rowconfigure(row+len(dirs)+1)
        Button(Frame_int3, text="Output Selected", command=lambda: print([f for f in dirs if data_vars[dirs.index(f)].get()]), name="output").place(relx=0.5, rely=0.5, anchor=CENTER)
    buttons = []
    Frame_int1 = Frame(Test, width=Test.winfo_reqwidth())
    Frame_int1.grid(row=0, sticky='nswe', columnspan=2)
    Frame_Checkboxes = Frame(Test)
    Frame_Checkboxes.grid(row=1, column=0, sticky='nswe')
    Frame_Config = Frame(Test)
    Frame_Config.grid(row=1, column=1, sticky='nswe')
    Frame_int3 = Frame(Test, width=Test.winfo_reqwidth())
    Frame_int3.grid(row=2, sticky= 'nswe', columnspan=2)
    Test.rowconfigure(0, weight=1)
    Test.rowconfigure(1, weight=8)
    Test.columnconfigure(1, weight=2)
    Test.columnconfigure(0, weight=8)
    Test.rowconfigure(2, weight=1)
    canvas = Canvas(Frame_Checkboxes)
    scrbr = Scrollbar(Frame_Checkboxes, orient=VERTICAL, command=canvas.yview)
    Checkboxes = Frame(canvas)
    canvas.configure(yscrollcommand=scrbr.set, scrollregion=canvas.bbox("all"))
    canvas.bind_all("<MouseWheel>", on_mousewheel)
    canvas.create_window((0, 0), window=Checkboxes, anchor="nw")
    Frame_Checkboxes.grid_rowconfigure(0, weight=1)
    Frame_Checkboxes.grid_columnconfigure(0, weight=1)
    canvas.grid(row=0, column=0, sticky='nswe')
    scrbr.grid(row=0, column=1, sticky='ns')
    ext_dir = StringVar(Test, name="ext_dir1")
    dirs = []
    data_vars = []
    ext_dir.trace_add('write', update_dirs_and_vars)
    ext_dir.set(filedialog.askdirectory(mustexist=True, title="选择目标文件夹"))
    Button(
        Frame_int1,
        text="Choose directory",
        command=lambda: (
            ext_dir.set(
                filedialog.askdirectory(mustexist=True, title="选择目标文件夹")
            ),
            Test.update()
        )
    ).place(relx=0.5, rely=0.5, anchor=CENTER)
    Checkboxes.update_idletasks()
    canvas.configure(scrollregion=canvas.bbox("all"))
    #region init
    if ext_dir.get() : 
        update_dirs_and_vars()