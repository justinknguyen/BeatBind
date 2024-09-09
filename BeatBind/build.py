import ttkthemes
import os

"""
Nuitka Build Command:
(replace '--standalone' with '--onefile' to create an .exe that does not depends on other files)

python -m nuitka ^
--standalone ^
--mingw64 ^
--windows-console-mode=disable ^
--include-package=tkinter ^
--include-package=ttkthemes ^
--enable-plugin=tk-inter ^
--include-data-file="icon.ico=./" ^
--windows-icon-from-ico=icon.ico ^
--output-dir="BeatBind" ^
--output-file=BeatBind.exe ^
--include-data-dir="{path}=ttkthemes" ^
app.py


PyInstaller Build Command:
pyinstaller --onefile --noconsole --add-data "icon.ico;." --icon=icon.ico -n "BeatBind" app.py
"""

# place the path in the last build command option if using Nuitka
path = print(os.path.dirname(ttkthemes.__file__))
