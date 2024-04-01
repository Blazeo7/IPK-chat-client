# Changelog

### Implemented functionality
- Added communication support for both TCP and UDP protocols.
- Implemented graceful handling of interruption signals for communication termination in the Udp variant.
- Introduced verbosity option `-v` for enhanced logging during application runtime.

### Known Limitations
- Disconnecting issue when communicating using TCP:
  - When attempting to disconnect from the server, the client sends a FIN message. However, the server does not respond with a corresponding FIN message, leading the client to send an RST flag.
