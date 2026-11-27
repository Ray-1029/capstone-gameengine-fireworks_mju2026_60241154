from pathlib import Path
import shutil,uuid,sys
sys.stdout.reconfigure(encoding='utf-8')
root=Path(__file__).resolve().parents[1]
name=sys.argv[1] if len(sys.argv)>1 else 'ParadeValidationProject'
if name not in {'ParadeValidationProject','WideValidationProject'}: raise ValueError('Unexpected validation project name')
target=root/'Verification'/name
for p in (root/'Assets/Festival').rglob('*.cs'):
    meta=Path(str(p)+'.meta')
    if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')
for folder in ['Assets','Packages','ProjectSettings','Analysis']:
    shutil.copytree(root/folder,target/folder,dirs_exist_ok=True)
(target/'Verification').mkdir(exist_ok=True)
print(target)
