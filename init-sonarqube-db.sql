-- Create SonarQube database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SonarQubeDb')
BEGIN
    CREATE DATABASE [SonarQubeDb];
    PRINT 'Database SonarQubeDb created successfully';
END
ELSE
BEGIN
    PRINT 'Database SonarQubeDb already exists';
END