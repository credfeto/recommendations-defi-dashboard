# Container Image Instructions

[Back to Local Instructions Index](index.md)

> Load when: the `Dockerfile` is changed, the runtime or build base image is changed, the server's command-line switches are changed, or a new service is added.

## Runtime Base Image (MANDATORY)

- Keep the Dockerfile runtime base image on the same Ubuntu release as the environment that publishes the native AOT binary. The binary links against the build host's glibc, so a runtime base with an older glibc cannot load it and the container restart-loops.
- When the build runner moves to a new Ubuntu release, move the runtime base (for example `mcr.microsoft.com/dotnet/runtime-deps:<version>-<codename>`) in the same change.
- Pin the base image by digest as well as tag, because Dependabot's `docker` ecosystem keeps both up to date.

## Build-Time Self-Test (MANDATORY)

Every service binary must support a `--self-test` switch, and the final image must run it during `docker build`, so a glibc or native-library mismatch fails the image build rather than production.

- `--self-test` builds the application with the same configuration and service registrations as a normal start, then:
  - resolves the dependency-injection graph, including every hosted service, without starting any of them;
  - loads every native dependency the service relies on (OpenSSL through a hash, ICU culture data, and any other native library the service uses);
  - binds no ports and opens no external connections;
  - exits `0` on success and non-zero on any failure, writing the failure to standard error.
- Keep the self-test native-AOT safe: resolve services with the generic `GetRequiredService<T>`/`GetServices<T>` methods, never with reflection over the service descriptors.
- Run the self-test in the final runtime image after the `COPY` of the published output and before `ENTRYPOINT`, using the exec form so it also works in distroless images with no shell:

  ```dockerfile
  RUN ["/usr/src/app/Credfeto.Defi.Server", "--self-test"]
  ```

- To check the step works, build once against a runtime base with an older glibc and confirm the build fails at the `RUN ... --self-test` step.
