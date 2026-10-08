"""Receive UTF-8 JSON drawing packets; run on the destination PC."""
import argparse
import json
import socket


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--bind', default='0.0.0.0')
    parser.add_argument('--port', type=int, default=5005)
    args = parser.parse_args()
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as receiver:
        receiver.bind((args.bind, args.port))
        print(f'Listening on {args.bind}:{args.port}', flush=True)
        try:
            while True:
                data, source = receiver.recvfrom(65535)
                try:
                    packet = json.loads(data.decode('utf-8'))
                    if not isinstance(packet, dict) or packet.get('version') != 1:
                        raise ValueError('unsupported packet')
                    print(json.dumps({'source': source, 'packet': packet}, ensure_ascii=False), flush=True)
                except (UnicodeDecodeError, ValueError) as error:
                    print(f'Ignored invalid packet from {source}: {error}', flush=True)
        except KeyboardInterrupt:
            pass


if __name__ == '__main__':
    main()
