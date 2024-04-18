from threading import Thread, main_thread

import time
import ctypes
from ctypes import wintypes
import win32con
import win32api

from .keycodes import vk_key_names, vk_non_modifier_codes


def _to_virtualkey(key):
    virtual_key = None
    if key.lower() in vk_key_names.keys():
        virtual_key = vk_key_names[key.lower()]
    return virtual_key

full_modifier_list = [
    _to_virtualkey("control"),
    _to_virtualkey("shift"),
    _to_virtualkey("alt"),
    _to_virtualkey("left_window"),
    _to_virtualkey("right_window"),
    _to_virtualkey("window"),
]

class EngineState:

    def __init__(self, active=True):
        self.active = active


class HotkeyChecker():

    def __init__(self):
        self.hotkeys = {}
        self.hotkey_actions = {}
        self.state = EngineState()
        self.hotkey_counter = 0
    
    def _find_hotkey_id(self, binding):
        for hotkey_id, _binding in self.hotkeys.items():
            match = False
            if _binding == binding:
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

    def _is_valid_binding(self, binding):
        for chord in binding:
            for key in chord:
                virtual_key = _to_virtualkey(key)
                # If the key doesn't exist, throw an exception to bring attention to it.
                if virtual_key == None:
                    if(isinstance(key, list)):
                        hotkey_string = str(key)
                        valid_hotkey_string = " + ".join(key)
                        raise Exception(
                            "You've specified the hotkey as a list. The syntax has changed to being specified as a string now.\n"+
                            f"Your hotkey {hotkey_string} should now be specified as \"{valid_hotkey_string}\""
                        )
                    raise Exception(
                        "The key [%s] not a valid virtual keystroke." % key)
                    return False
    
    def remove_hotkey(self, binding):
        self._is_valid_binding(binding)
        
        hotkey_id = self._find_hotkey_id(binding)
        if hotkey_id == None:
            return False
        
        del self.hotkeys[hotkey_id]
        del self.hotkey_actions[hotkey_id]

        return True

    def _reset_binding_press_state(self, hotkey_id):
        for i in range(0, len(self.hotkey_actions[hotkey_id][2])):
            self.hotkey_actions[hotkey_id][2][i] = 0

    def register_hotkey(self, binding, press_callback, release_callback, actuate_on_partial_release, press_callback_params, release_callback_params):
        self._is_valid_binding(binding)
        
        self.hotkey_counter += 1
        id = self.hotkey_counter

        hotkey_id = self._find_hotkey_id(binding)
        if hotkey_id != None:
            raise Exception(
                "The hotkey [%s] is already registered." % str(binding)
            )
            return False

        # we want to track how far along the binding's chording sequence we've progressed.
        binding_press_state = [0 for i in range(0, len(binding))]

        self.hotkeys[id] = binding
        self.hotkey_actions[id] = [press_callback, release_callback, binding_press_state, actuate_on_partial_release, press_callback_params, release_callback_params]

        return True
    
    def _find_index_of_first_item_not_matching_in_list(self, _list, target):
        for i in range(0, len(_list)):
            if _list[i] != target:
                return i
        return None

    def _are_all_keys_pressed_in_chord(self, chord):
        for key in chord:
            if key == "window":
                lwin_key_state = win32api.GetAsyncKeyState(win32con.VK_LWIN)
                rwin_key_state = win32api.GetAsyncKeyState(win32con.VK_RWIN)
                if (lwin_key_state >= 0) and (rwin_key_state >= 0):
                    return False
            else:
                specific_key_state = win32api.GetAsyncKeyState(key)
                if specific_key_state >= 0:
                    return False
        return True
    
    def _are_any_keys_pressed_in_chord(self, chord):
        for key in chord:
            if key == "window":
                continue
            else:
                specific_key_state = win32api.GetAsyncKeyState(key)
                if specific_key_state < 0:
                    return True
        return False
    
    def _are_all_keys_not_pressed_in_chord(self, chord):
        for key in chord:
            if key == "window":
                lwin_key_state = win32api.GetAsyncKeyState(win32con.VK_LWIN)
                rwin_key_state = win32api.GetAsyncKeyState(win32con.VK_RWIN)
                if (lwin_key_state < 0) or (rwin_key_state < 0):
                    return False
            else:
                specific_key_state = win32api.GetAsyncKeyState(key)
                if specific_key_state < 0:
                    return False
        return True

    def _get_chord_state(self, chord):
        result = {}
        #chord = [_to_virtualkey(key) if key != "window" else "window" for key in _chord]
        for _key in chord:
            key = _to_virtualkey(_key) if _key != "window" else "window"
            result[_key] = str(False)
            print(_key)
            if key == "window":
                lwin_key_state = win32api.GetAsyncKeyState(win32con.VK_LWIN)
                rwin_key_state = win32api.GetAsyncKeyState(win32con.VK_RWIN)
                if (lwin_key_state < 0) or (rwin_key_state < 0):
                    result[_key] = str(True)
            else:
                specific_key_state = win32api.GetAsyncKeyState(key)
                if specific_key_state < 0:
                    result[_key] = str(True)
        return result

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
                press_callback, release_callback, binding_press_state, actuate_on_partial_release, press_callback_params, release_callback_params = self.hotkey_actions[id]
                key_state_id = self._find_index_of_first_item_not_matching_in_list(binding_press_state, 2)
                if(key_state_id is None):
                    raise Exception("binding_press_state was not reset after completion!")
                    continue
                key_state = binding_press_state[key_state_id]
                chord = [_to_virtualkey(key) if key != "window" else "window" for key in hotkey[key_state_id]]

                non_allowed_keys = [key for key in vk_non_modifier_codes if key not in chord]

                # Check to see if an active chord is being broken
                if((key_state_id > 0) and (self._are_any_keys_pressed_in_chord(non_allowed_keys))):
                    self._reset_binding_press_state(id)
                    continue
                # check to see if all keys in the hotkey are pressed.
                pressed = self._are_all_keys_pressed_in_chord(chord)
                fully_not_pressed = self._are_all_keys_not_pressed_in_chord(chord)
                
                # ensure that modifiers not in this hotkey aren't pressed
                non_allowed_modifiers = []
                for key in full_modifier_list:
                    if key not in chord:
                        if((key == "window") and ((_to_virtualkey("left_window") in chord) or _to_virtualkey("right_window") in chord)):
                            continue
                        if(key == _to_virtualkey("left_window")) and ("window" in chord):
                            continue
                        if(key == _to_virtualkey("right_window")) and ("window" in chord):
                            continue
                        non_allowed_modifiers.append(key)
                
                if pressed:
                    pressed = self._are_all_keys_not_pressed_in_chord(non_allowed_modifiers)

                if pressed:
                    this_is_the_last_chord = key_state_id == len(hotkey) - 1
                    self.hotkey_actions[id][2][key_state_id] = 1
                    if (key_state != 1) and (this_is_the_last_chord):
                        if press_callback != None:
                            if press_callback_params != None:
                                press_callback(press_callback_params)
                            else:
                                press_callback()
                else:
                    this_is_the_last_chord = key_state_id == len(hotkey) - 1
                    #self.hotkey_actions[id][2] = False
                    if (key_state == 1):
                        if this_is_the_last_chord:
                            if fully_not_pressed or actuate_on_partial_release:
                                self._reset_binding_press_state(id)
                                if release_callback != None:
                                    if release_callback_params != None:
                                        release_callback(release_callback_params)
                                    else:
                                        release_callback()
                        else:
                            self.hotkey_actions[id][2][key_state_id] = 2

hotkey_checker = HotkeyChecker()