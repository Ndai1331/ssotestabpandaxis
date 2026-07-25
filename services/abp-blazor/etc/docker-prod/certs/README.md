# Place production OpenIddict signing certificate here:
#   openiddict.pfx
#
# Generate (example):
#   openssl req -x509 -newkey rsa:4096 -sha256 -days 825 \
#     -keyout openiddict.key -out openiddict.crt -nodes \
#     -subj "/CN=hanhchinhso-authserver"
#   openssl pkcs12 -export -out openiddict.pfx \
#     -inkey openiddict.key -in openiddict.crt \
#     -passout pass:'YOUR_PASSPHRASE'
#
# AUTHSERVER_CERTIFICATE_PASSPHRASE in .env must match.
# Never commit the real .pfx file.
