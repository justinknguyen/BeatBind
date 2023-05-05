import ttkthemes
import os

path = print(os.path.dirname(ttkthemes.__file__)) # place the path in the last build command option

'''
Build Command:

python -m nuitka ^
--onefile ^
--mingw64 ^
--disable-console ^
--include-package=tkinter ^
--include-package=ttkthemes ^
--enable-plugin=tk-inter ^
--include-data-file="icon.ico=./" ^
--windows-icon-from-ico=icon.ico ^
--output-file=SpotifyGlobalHotkeys.exe ^
--include-data-dir="{path}=ttkthemes" ^
app.py

'''
