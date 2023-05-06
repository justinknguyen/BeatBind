from threading import Thread, main_thread

import time
import ctypes
from ctypes import wintypes
import win32con
import win32api

from .keycodes import vk_key_names

def _to_virtualkey(key):
    virtual_key = None
    if key.lower() in vk_key_names.keys():
        virtual_key = vk_key_names[key.lower()]
    return virtual_key

class EngineState:

    def __init__(self, active=True):
        self.active = active


class HotkeyChecker():

    def __init__(self):
        self.hotkeys = {}
        self.hotkey_actions = {}
        self.state = EngineState()
        self.hotkey_counter = 0
    
    def _find_hotkey_id(self, hotkey):
        for hotkey_id, hotkey_modifiers in self.hotkeys.items():
            all_match = False
            if len(hotkey_modifiers) == len(hotkey):
                all_match = True
                for modifier in hotkey:
                    if modifier not in hotkey_modifiers:
                        all_match = False
                        break
            if all_match:
                return hotkey_id
        return None

    def start_checking_hotkeys(self):
        self.state.active = True
        Thread(target=self.run).start()

    def shutdown_checker(self):
        self.state.active = False
        self.state = EngineState(False)

    def restart_checker(self):
        self.shutdown_checker()
        time.sleep(0.7) # bit of a magic number here, just letting the old run thread die out before starting a fresh one.
        self.start_checking_hotkeys()

    def clear_bindings(self):
        self.shutdown_checker()
        self.hotkeys.clear()
        self.hotkey_actions.clear()
        self.hotkey_counter = 0
    
    def remove_hotkey(self, key, modifiers):
        virtual_key = _to_virtualkey(key)

        # If the key doesn't exist, throw an exception to bring attention to it.
        if virtual_key == None:
            raise Exception(
                "The key [%s] not a valid virtual keystroke." % key)
            return False
        
        hotkey_id = self._find_hotkey_id(modifier_list)
        if hotkey_id == None:
            return False
        
        del self.hotkeys[hotkey_id]
        del self.hotkey_actions[hotkey_id]

        return True

    def register_hotkey(self, key, modifiers, press_callback, release_callback=None):
        virtual_key = _to_virtualkey(key)

        # If the key doesn't exist, throw an exception to bring attention to it.
        if virtual_key == None:
            raise Exception(
                "The key [%s] not a valid virtual keystroke." % key)
            return False
        
        self.hotkey_counter += 1
        id = self.hotkey_counter

        modifier_list = [virtual_key, ]
        for modifier in modifiers:
            modifier_list.append(vk_key_names[modifier.lower()])

        if self._find_hotkey_id(modifier_list) != None:
            return False

        self.hotkeys[id] = tuple(modifier_list)
        self.hotkey_actions[id] = [press_callback, release_callback, False]

        return True

    def run(self):
        state = self.state
        id_list = self.hotkeys.keys()

        while state.active:
            time.sleep(0.02)
            # Exit out if the main thread has terminated.
            if not main_thread().is_alive():
                break
            
            for id in id_list:
                hotkey = self.hotkeys[id]
                press_callback, release_callback, key_state = self.hotkey_actions[id]

                # check to see if all keys in the hotkey are pressed.
                pressed = True
                for key in hotkey:
                    specific_key_state = win32api.GetAsyncKeyState(key)
                    if specific_key_state >= 0:
                        pressed = False
                        break
                
                if pressed:
                    self.hotkey_actions[id][2] = True
                    if not key_state:
                        if press_callback != None:
                            press_callback()
                else:
                    self.hotkey_actions[id][2] = False
                    if key_state:
                        if release_callback != None:
                            release_callback()

hotkey_checker = HotkeyChecker()