# Use the official .NET 9.0 runtime as a base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
# Expose additional ports for the container
EXPOSE 5000
EXPOSE 5002
# Expose port 1234 for LM Studio communication
EXPOSE 1234

# Use the .NET 9.0 SDK for building the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ./ ./
RUN dotnet restore PhotoStorage.csproj

# Build the application
WORKDIR /src
RUN dotnet publish PhotoStorage.csproj -c Release -o /app/publish

# Install Python in the container
RUN apt-get update && apt-get install -y python3 python3-pip && rm -rf /var/lib/apt/lists/*

# Install python3-venv to enable virtual environment creation
RUN apt-get update && apt-get install -y python3-venv && rm -rf /var/lib/apt/lists/*

# Set Python3 as the default python
RUN ln -s /usr/bin/python3 /usr/bin/python

# Copy the requirements.txt file
COPY Services/UserActivity/requirements.txt /app/UserActivity/requirements.txt

# Create a Python virtual environment and install dependencies
RUN python3 -m venv /app/venv \
    && /app/venv/bin/pip install --upgrade pip \
    && /app/venv/bin/pip install -r /app/UserActivity/requirements.txt

# Set the virtual environment as the default for Python
ENV PATH="/app/venv/bin:$PATH"

# Final stage: Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
# Copy Python scripts into the container
COPY Services/UserActivity/ /app/UserActivity/
ENV ASPNETCORE_ENVIRONMENT Development
ENTRYPOINT ["dotnet", "PhotoStorage.dll"]