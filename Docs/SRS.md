# Software Requirements Specification (SRS)
## 1. Introduction
### 1.1 Purpose
The purpose of this document is to define the software requirements for the Student Information Management System (SIMS). This system is designed to manage university-level information including students, courses, enrollments, faculties, and academic records.

### 1.2 Scope
SIMS will provide a centralized web-based platform. Key features include:
- Student Registration & Profile Management.
- Course Management (creation, credit rules, prerequisites).
- Enrollment Management (registering courses, timetable collision checks).
- Academic Records (grades, GPA/CPA calculations).
- Role-based Authentication & Authorization (Admin, Faculty, Student).

## 2. Overall Description
### 2.1 User Characteristics
- **Administrator**: Full access to all modules. Manages users, faculty, students, and courses.
- **Faculty**: Can view assigned courses, student lists, and enter grades.
- **Student**: Can view profile, register for courses, view timetable, and check grades.

### 2.2 Operating Environment
- Server: Windows Server or Linux hosting IIS or Kestrel.
- Database: Microsoft SQL Server.
- Client: Modern web browsers (Chrome, Firefox, Safari, Edge).

## 3. System Features (Functional Requirements)
### 3.1 Student Management (Admin)
- System shall allow Admin to register new students.
- System shall support soft-delete and restoration of student records.
- System shall allow Admin to view and edit student profiles.

### 3.2 Course Management (Admin)
- System shall allow Admin to create, update, and soft-delete courses.
- System shall allow defining credits, department, prerequisites, and assigned faculty.

### 3.3 Enrollment Management (Student/Admin)
- System shall allow Students to register/unregister for active courses.
- System shall validate prerequisites before enrollment.
- System shall check for schedule conflicts and credit limits.

### 3.4 Grade Management (Faculty/Admin)
- System shall allow Faculty to input grades for their assigned courses.
- System shall automatically calculate GPA (semester) and CPA (cumulative).

### 3.5 Authentication and Security
- System shall use ASP.NET Core Identity for login/logout.
- System shall support role-based and policy-based authorization.
- Passwords must be hashed.

## 4. Non-Functional Requirements
- **Performance**: Pages should load within 2 seconds.
- **Security**: Prevent SQL Injection, XSS, and CSRF. HTTPS required.
- **Architecture**: Follow Clean Architecture and SOLID principles.
- **UI/UX**: Responsive design using Bootstrap 5.
