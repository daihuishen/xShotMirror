"""Check whether the local Bonjour service sees xShot Mirror's AirPlay record."""

import ctypes
import threading
import time


def main() -> int:
    dll = ctypes.WinDLL("dnssd.dll")
    ref = ctypes.c_void_p()
    found = threading.Event()

    callback_type = ctypes.WINFUNCTYPE(
        None, ctypes.c_void_p, ctypes.c_uint32, ctypes.c_uint32,
        ctypes.c_int32, ctypes.c_char_p, ctypes.c_char_p, ctypes.c_char_p,
        ctypes.c_void_p)

    @callback_type
    def on_service(_ref, flags, _interface, error, name, _kind, _domain, _context):
        if error == 0 and flags & 2 and name and name.decode("utf-8", "replace") == "xShot Mirror":
            found.set()

    dll.DNSServiceBrowse.argtypes = [ctypes.POINTER(ctypes.c_void_p), ctypes.c_uint32,
                                    ctypes.c_uint32, ctypes.c_char_p, ctypes.c_char_p,
                                    callback_type, ctypes.c_void_p]
    dll.DNSServiceBrowse.restype = ctypes.c_int32
    dll.DNSServiceProcessResult.argtypes = [ctypes.c_void_p]
    dll.DNSServiceProcessResult.restype = ctypes.c_int32
    dll.DNSServiceRefDeallocate.argtypes = [ctypes.c_void_p]

    error = dll.DNSServiceBrowse(ctypes.byref(ref), 0, 0, b"_airplay._tcp", None,
                                 on_service, None)
    if error:
        print(f"Bonjour browse failed with code {error}.")
        return 2

    def process_events() -> None:
        while not found.is_set():
            if dll.DNSServiceProcessResult(ref) != 0:
                break

    thread = threading.Thread(target=process_events, daemon=True)
    thread.start()
    found.wait(6)
    dll.DNSServiceRefDeallocate(ref)
    if found.is_set():
        print("Bonjour discovered xShot Mirror. iPhone discovery still needs a real-device test.")
        return 0
    print("Bonjour did not discover xShot Mirror within six seconds.")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
