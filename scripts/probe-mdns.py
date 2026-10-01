"""Probe local AirPlay service advertisements without capturing mirror content."""

import argparse
import socket
import struct
import time


def query_packet(service: str) -> bytes:
    labels = b"".join(bytes([len(part)]) + part.encode("ascii") for part in service.split("."))
    # Request a unicast reply so this probe need not compete with receivers on UDP 5353.
    return struct.pack("!HHHHHH", 0, 0, 1, 0, 0, 0) + labels + b"\0" + struct.pack("!HH", 12, 0x8001)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--interface", default="0.0.0.0", help="Windows LAN IPv4 address")
    parser.add_argument("--seconds", type=float, default=5.0)
    parser.add_argument("--verbose", action="store_true")
    args = parser.parse_args()

    service = "_airplay._tcp.local"
    group = "224.0.0.251"
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM, socket.IPPROTO_UDP)
    sock.bind((args.interface, 0))
    sock.setsockopt(socket.IPPROTO_IP, socket.IP_MULTICAST_IF, socket.inet_aton(args.interface))
    sock.setsockopt(socket.IPPROTO_IP, socket.IP_MULTICAST_TTL, 1)
    sock.settimeout(0.5)
    sock.sendto(query_packet(service), (group, 5353))

    deadline = time.monotonic() + args.seconds
    count = 0
    while time.monotonic() < deadline:
        try:
            data, sender = sock.recvfrom(9000)
        except socket.timeout:
            continue
        if args.verbose:
            print(f"packet from {sender[0]} ({len(data)} bytes): "
                  f"airplay={b'_airplay' in data}, name={b'xShot Mirror' in data}")
        if b"xShot Mirror" in data and b"_airplay" in data:
            print(f"AirPlay advertisement containing xShot Mirror from {sender[0]}")
            count += 1
    sock.close()
    if count:
        print(f"Observed {count} matching mDNS packet(s). This does not prove iPhone discovery.")
        return 0
    print("No matching mDNS packet observed; check interface, firewall, and receiver status.")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
