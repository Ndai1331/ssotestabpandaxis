# Signing SDK release blocker

The service exposes `IDigitalSigningAdapter` but ships only the deterministic development adapter. Do not package or deploy `Bnn.SignLib`, `Bnn.Sdk`, native binaries, or derived code until the owner supplies written redistribution rights and a security review approves the integration. CI/release policy must treat unresolved adapter id `bnn` as a blocking condition.
