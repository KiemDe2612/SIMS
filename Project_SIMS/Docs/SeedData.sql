-- SEED DATA CHO DỰ ÁN SIMS
-- Lưu ý: Password hashes ở đây chỉ là chuỗi mô phỏng cho mật khẩu 'Admin@123', 'Faculty@123', 'Student@123'. 
-- Trong thực tế khi ứng dụng chạy, nên dùng tính năng UserManager của ASP.NET Core Identity để seed thay vì dùng raw SQL.

USE [SIMSDb];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Thêm Roles
INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) 
VALUES 
(NEWID(), 'Administrator', 'ADMINISTRATOR', NEWID()),
(NEWID(), 'Faculty', 'FACULTY', NEWID()),
(NEWID(), 'Student', 'STUDENT', NEWID());

-- 2. Thêm Department
DECLARE @DeptId UNIQUEIDENTIFIER = NEWID();
INSERT INTO [Departments] ([Id], [Code], [Name], [Description], [CreatedAt], [IsDeleted])
VALUES 
(@DeptId, 'SE', 'Software Engineering', 'Khoa Kỹ thuật Phần mềm', GETUTCDATE(), 0);

-- 3. Thêm Course
DECLARE @CourseId UNIQUEIDENTIFIER = NEWID();
INSERT INTO [Courses] ([Id], [Code], [Title], [Description], [Credits], [DepartmentId], [CreatedAt], [IsDeleted])
VALUES 
(@CourseId, 'PRN211', 'Basic Cross-Platform Application Programming With .NET', 'Lập trình C# cơ bản', 3, @DeptId, GETUTCDATE(), 0);

-- 4. Thêm Semester
DECLARE @SemId UNIQUEIDENTIFIER = NEWID();
INSERT INTO [Semesters] ([Id], [Name], [StartDate], [EndDate], [IsActive], [CreatedAt], [IsDeleted])
VALUES 
(@SemId, 'Fall 2026', '2026-09-01', '2026-12-31', 1, GETUTCDATE(), 0);

PRINT 'Seed data completed successfully.';
