# Hypertext Transfer Protocol (HTTP) - Summary

## Overview

HTTP (Hypertext Transfer Protocol) is the primary transport protocol for web-based content, developed by Tim Berners-Lee. Its success stems from its simplicity and request-response architecture.

- **Core Principle**: Client sends a request → Server processes it → Server returns a response
- **Transport**: Typically uses TCP on port 80
- **Key Advantage**: Backward compatible (HTTP 1.1 clients can communicate with HTTP 1.0 servers)

---

## History

| Version | Features |
|---------|----------|
| **HTTP 0.9** | Simple transport, only GET method, no MIME or authentication |
| **HTTP 1.0** (RFC 1945) | Extended request/response format, headers, POST method |
| **HTTP 1.1** (RFC 2616) | Persistent connections, request pipelining, cache control |

---

## HTTP Connection Process

1. Client and server establish a TCP connection (port 80)
2. Messages (Request/Response) consist of:
   - **Header** – Control information (method, URL, etc.)
   - **Data** – HTML documents, form data, etc.

### Persistent Connections (HTTP 1.1)
- Multiple requests can use the same connection
- Request pipelining: Client can send subsequent requests before receiving previous responses
- Significant performance improvement

---

## HTTP Request Structure

```
METHOD URL HTTP/version
General Headers
Request Headers
Entity Headers (optional)
[blank line]
Request Entity (if present)
```

### Example Request
```
GET /verzeichnis1/seite2.html HTTP/1.1
Date: Thursday, 14-Oct-99 17:55 GMT
User-agent: Mozilla/4.6
Accept: text/html, text/plain
```

---

## HTTP Response Structure

```
HTTP/version Status-Code Reason-Phrase
General Headers
Response Headers
Entity Headers (optional)
[blank line]
Resource Entity (if present)
```

### Example Response
```
HTTP/1.1 200 OK
Via: HTTP/1.1 proxy_server_name
Server: Apache/1.3
Content-type: text/html
Content-length: 78

<html>
<head><title>HTTP</title></head>
<body><p>HTTP/1.1-Demo</p></body>
</html>
```

**Important Headers:**
- `Content-type` – MIME type of the data
- `Content-length` – Data size in bytes (recommended by RFC)

---

## Response Status Codes

| Category | Meaning |
|----------|---------|
| **1xx** | Informational – request received, processing continues |
| **2xx** | Success – request successfully received and accepted |
| **3xx** | Redirection – further actions needed |
| **4xx** | Client error – invalid syntax or cannot be processed |
| **5xx** | Server error – server cannot process valid request |

**Common codes:** `200 OK`, `403 Forbidden`, `404 Not Found`

---

## HTTP Methods

| Method | Description |
|--------|-------------|
| **GET** | Request a document/resource (most important). Types: conditional GET, partial GET |
| **POST** | Submit form data, comments, forum messages, database entries |
| **OPTIONS** | Query available communication options without action/data transfer |
| **HEAD** | Request only headers (no data) – useful for checking size/type |
| **PUT** | Modify existing or create new data on server |
| **DELETE** | Delete a resource |
| **TRACE** | Diagnostic tracing |
| **CONNECT** | For proxy tunneling |

---

## Authentication Methods

### Basic Authentication
- Username and password sent in **Base64 encoding** (no encryption)
- Must be sent with every request
- **Weakness:** No encryption of user credentials

### Digest Access Authentication (RFC 2617)
- Encrypts credentials using **MD5** algorithm
- Uses parameters: username, password, HTTP method, requested URL, random values (nonce)
- More secure than Basic Authentication

**Server Challenge Example:**
```
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Digest realm="testrealm@host.com", 
    qop="auth, auth-int", nonce="dcd98b7102dd2f0e8b11d0f600bfb0c093"
```

**Client Response Example:**
```
Authorization: Digest username="Benutzername", 
    realm="testrealm@host.com", response="6629fae49393a05397450978507c4ef1"
```

---

## Practical Example: Loading an HTML Page with Images

HTTP 1.1 cannot send an HTML file and its embedded images simultaneously. The browser:

1. Requests the HTML file (e.g., `title.html`)
2. Parses the received HTML
3. Sends **separate requests** for each embedded resource (images, etc.)

### Example HTML (`hta_test.html`):
```html
<html>
<head><title>Anzeige von Bildern</title></head>
<body background="pictures/bg.gif">
    <img src="pictures/logo.jpg">
</body>
</html>
```

**Resulting HTTP requests:**
1. Request for `demo/picture.html` (HTML page)
2. Request for `pictures/bg.gif` (background image)
3. Request for `pictures/logo.jpg` (foreground image)
