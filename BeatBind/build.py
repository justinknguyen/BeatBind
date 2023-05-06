import ttkthemes
import os

# place the path in the last build command option if using Nuitka
path = print(os.path.dirname(ttkthemes.__file__)) 

'''
Nuitka Build Command:

python -m nuitka ^
--onefile ^
--mingw64 ^
--disable-console ^
--include-package=tkinter ^
--include-package=ttkthemes ^
--enable-plugin=tk-inter ^
--include-data-file="icon.ico=./" ^
--windows-icon-from-ico=icon.ico ^
--output-file=BeatBind.exe ^
--include-data-dir="{path}=ttkthemes" ^
app.py
'''

'''
PyInstaller Build Command:

pyinstaller --onefile --noconsole --add-data "icon.ico;." --icon=icon.ico -n "BeatBind" app.py
'''
