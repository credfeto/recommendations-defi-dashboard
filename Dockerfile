# The runtime base must be on the same Ubuntu release as the build environment: the native AOT binary links
# against the build host's glibc and cannot load on an older one.
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-resolute@sha256:0b3fc2ffc701d6bb341a8112cd37edb148227920185dc2912fb3d49a86689ea5

WORKDIR /usr/src/app

# Bundle App Source
COPY Credfeto.Defi.Server .
COPY appsettings.json .

# hadolint ignore=DL3008
RUN apt-get update && apt-get upgrade -y && apt-get install curl -y --no-install-recommends && apt-get autoremove -y && apt-get clean && rm -rf /var/lib/apt/lists/*

# Fail the image build, rather than production, if the binary cannot load its native dependencies here
RUN ["/usr/src/app/Credfeto.Defi.Server", "--self-test"]

EXPOSE 8080
EXPOSE 8081
ENTRYPOINT [ "/usr/src/app/Credfeto.Defi.Server" ]

# Perform a healthcheck.  note that ECS ignores this, so this is for local development
HEALTHCHECK --interval=5s --timeout=2s --retries=3 --start-period=15s \
  CMD ["/usr/src/app/Credfeto.Defi.Server", "--health-check", "http://127.0.0.1:8080/ping"]
