# 用于Rimworld mod制作及翻译之类的Python脚本

## Translation Cleaner
Rimtrans唯一的问题就是有时导出的太多，能用的太少。单复数啊，对不同年龄阶段的不同叫法啊，机械族竟然也分公母啊……这个脚本就是用来把Rimtrans导出来的“多余”词条全部删掉的


## Translation Extractor
Rimtrans还有一个东西它处理不了：在Patch内对Def的label、description之类做出的修改。这个脚本可以提取出这一部分会被Rimtrans落下的文本。


## PatchOperationGenerator
想自制XML mod却无从下手？知道该改哪部分却不知道怎么写补丁，只能直接对mod本身做出修改？这个脚本就是为了这部分人群定做的。先复制一份原mod的备份，然后在原mod上做出你想做出的修改，再用这个脚本生成对应的PatchOperation即可。