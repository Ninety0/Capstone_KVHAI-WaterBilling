# Modern Water Billing System for Katarungan Village  
Copyright and Academic Integrity Notice  
© 2025 Nick Dynel M. Avenido, Vince C. Doroja, Arman C. Lepaopao. All Rights Reserved.

This system was developed as an original capstone research project presented to the Faculty of the College of Information Technology and Computer Studies at Pamantasan ng Lungsod ng Muntinlupa.

**Authors:**  
AVENIDO, NICK DYNEL M.  
DOROJA, VINCE C.  
LEPAOPAO, ARMAN C.  

## IMPORTANT:  
This work is protected by copyright law and academic integrity policies.  
Unauthorized copying, reproduction, or plagiarism of this work is strictly prohibited.  
This includes, but is not limited to:  
- Direct copying of code  
- Reproducing documentation  
- Reusing database structures  
- Copying system architecture  
- Replicating business logic  

Academic use must properly cite this work.  
Commercial use or redistribution is not permitted without explicit written permission.  

For academic citations, please use:  
Avenido, N. D. M., Doroja, V. C., & Lepaopao, A. C. (2025). "DEVELOPMENT OF A MODERN WATER BILLING SYSTEM: REVOLUTIONIZING WATER BILLING PROCESS AT KATARUNGAN VILLAGE." A Capstone Project Presented to the Faculty of the College of Information Technology and Computer Studies, Pamantasan ng Lungsod ng Muntinlupa.

## Project Overview
This web-based water billing system was developed to modernize the billing process in Katarungan Village. The project addresses the inefficiencies of the current manual billing system by automating water consumption calculations, improving data accuracy, and streamlining payment processes.

## Research Objectives
1. Automate the calculation of water bills
2. Implement cubic consumption forecasting to track water usage trends
3. Provide real-time announcements for residents
4. Create login portals for homeowners and renters
5. Centralize the database for improved collaboration
6. Integrate PayPal for online payments

## Key Features
### Resident Portal
- View water consumption and billing history
- Receive real-time announcements
- Make online payments via PayPal
- Partial payment functionality
- Consumption forecasting visualization

### Staff Modules
- **Water Works:** Input meter readings via mobile devices
- **Clerk:** Generate bills and monitor reading progress
- **Cashier (Online):** Process and verify online payments
- **Cashier (Offline):** Record manual payments
- **Admin:** Manage accounts, announcements, and system monitoring

### Technical Features
- Automated billing computation
- Centralized database management
- Role-based access control
- Data encryption and security measures
- Moving average forecasting algorithm
- Responsive web design

## Technology Stack
### Backend:
- ASP.NET Core MVC (C#)
- Microsoft SQL Server
- IIS Web Server

### Frontend:
- HTML5/CSS3
- JavaScript (ES6+)
- Bootstrap 5
- jQuery
- AJAX

### Tools & Libraries:
- Visual Studio 2022 IDE
- SQL Server Management Studio
- Figma for UI/UX design
- NGROK for secure tunneling

## Current Limitations
1. Online payment limited to PayPal only
2. Requires stable internet connection
3. Manual meter reading input
4. Basic forecasting algorithm (3-5 month moving average)
5. Focused only on water billing (excludes other village payments)

## Setup and Installation
### Prerequisites:
- Visual Studio 2022
- SQL Server 2019+
- .NET Core 6.0 SDK
- IIS Web Server

### Installation Steps:
1. Clone the repository
2. Open solution in Visual Studio
3. Restore NuGet packages
4. Configure connection string in appsettings.json
5. Run database migrations
6. Build and run the application

## Documentation
Full technical documentation and research paper are available in the project files.

## Academic Documentation
- Complete capstone manuscript: `/documentation/The final manuscript April 28, 2025.pdf`
- System diagrams and flowcharts
- Evaluation tools and results
- Implementation plan

## About
This project represents a significant technological advancement for Katarungan Village, providing a more efficient, transparent, and user-friendly billing experience. Developed with scalability, security, and user experience in mind.
