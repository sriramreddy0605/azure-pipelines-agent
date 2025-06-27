#!/usr/bin/env python3
import socket
import threading
import base64
import hashlib

# Valid credentials
VALID_USERS = {
    "enterpriseuser": "enterprise123"
}

def handle_client(client_socket, client_address):
    try:
        # Receive client request
        request = client_socket.recv(4096).decode('utf-8')
        
        if not request:
            client_socket.close()
            return
            
        lines = request.split('\r\n')
        first_line = lines[0]
        
        # Check for Proxy-Authorization header
        auth_header = None
        for line in lines:
            if line.lower().startswith('proxy-authorization:'):
                auth_header = line.split(':', 1)[1].strip()
                break
        
        # If no auth header, send 407
        if not auth_header:
            response = """HTTP/1.1 407 Proxy Authentication Required\r
Server: Custom-Enterprise-Proxy/1.0\r
Proxy-Authenticate: Basic realm="Enterprise Pre-Auth Proxy"\r
Content-Type: text/html\r
Content-Length: 97\r
Connection: close\r
\r
<html><body><h1>407 Proxy Authentication Required</h1><p>Authentication required.</p></body></html>"""
            client_socket.send(response.encode())
            client_socket.close()
            return
            
        # Validate credentials
        if auth_header.startswith('Basic '):
            try:
                encoded_creds = auth_header[6:]
                decoded_creds = base64.b64decode(encoded_creds).decode('utf-8')
                username, password = decoded_creds.split(':', 1)
                
                if username in VALID_USERS and VALID_USERS[username] == password:
                    # Forward request to actual target
                    forward_request(client_socket, request, first_line)
                    return
            except:
                pass
        
        # Invalid credentials - send 407 again (pre-auth behavior)
        response = """HTTP/1.1 407 Proxy Authentication Required\r
Server: Custom-Enterprise-Proxy/1.0\r
Proxy-Authenticate: Basic realm="Enterprise Pre-Auth Proxy"\r
Content-Type: text/html\r
Content-Length: 97\r
Connection: close\r
\r
<html><body><h1>407 Proxy Authentication Required</h1><p>Invalid credentials.</p></body></html>"""
        client_socket.send(response.encode())
        client_socket.close()
        
    except Exception as e:
        print(f"Error handling client {client_address}: {e}")
        client_socket.close()

def forward_request(client_socket, original_request, first_line):
    try:
        # Extract target from CONNECT or GET request
        if first_line.startswith('CONNECT'):
            # HTTPS request
            target_info = first_line.split()[1]
            host, port = target_info.split(':')
            port = int(port)
        else:
            # HTTP request - extract from URL
            url = first_line.split()[1]
            if url.startswith('http://'):
                url = url[7:]
                if '/' in url:
                    host = url.split('/')[0]
                else:
                    host = url
                port = 80
            else:
                client_socket.send(b"HTTP/1.1 400 Bad Request\r\n\r\n")
                client_socket.close()
                return
        
        # Connect to target server
        server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        server_socket.connect((host, port))
        
        if first_line.startswith('CONNECT'):
            # Send 200 Connection Established for HTTPS
            client_socket.send(b"HTTP/1.1 200 Connection Established\r\n\r\n")
            # Start tunneling
            tunnel_data(client_socket, server_socket)
        else:
            # Forward HTTP request
            server_socket.send(original_request.encode())
            # Relay response back
            response = server_socket.recv(4096)
            client_socket.send(response)
            
        server_socket.close()
        client_socket.close()
        
    except Exception as e:
        print(f"Error forwarding request: {e}")
        client_socket.close()

def tunnel_data(client_socket, server_socket):
    def forward_data(source, destination):
        try:
            while True:
                data = source.recv(4096)
                if not data:
                    break
                destination.send(data)
        except:
            pass
        finally:
            source.close()
            destination.close()
    
    # Start forwarding in both directions
    threading.Thread(target=forward_data, args=(client_socket, server_socket)).start()
    threading.Thread(target=forward_data, args=(server_socket, client_socket)).start()

def main():
    # Create listening socket
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server_socket.bind(('0.0.0.0', 8091))
    server_socket.listen(5)
    
    print("Enterprise Pre-Auth Proxy listening on port 8091...")
    print("Credentials: enterpriseuser:enterprise123")
    
    try:
        while True:
            client_socket, client_address = server_socket.accept()
            print(f"Connection from {client_address}")
            threading.Thread(target=handle_client, args=(client_socket, client_address)).start()
    except KeyboardInterrupt:
        print("\nShutting down proxy...")
        server_socket.close()

if __name__ == "__main__":
    main()