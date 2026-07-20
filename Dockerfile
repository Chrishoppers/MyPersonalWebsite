FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 复制项目文件
COPY MyPersonalWebsite.csproj .
RUN dotnet restore

# 复制所有文件并构建
COPY . .
RUN dotnet publish -c Release -o /app/publish

# 运行环境
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# 暴露端口
EXPOSE 80
EXPOSE 443

# 启动命令
ENTRYPOINT ["dotnet", "MyPersonalWebsite.dll"]
