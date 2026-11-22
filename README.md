# 🌸 FloreHome

**Florist shop management CRUD** built with C# (Windows Forms, .NET 9) and MySQL.

It manages products, customers, users and invoices. The special touch: when you upload a flower photo, the system can turn it into a fixed-size **pixel art** picture that is later shown as a card menu when invoicing.

## Features

- **Login with roles** (Administrator / Employee). There is no public sign-up: the administrator creates the users, and anyone who is not an administrator doesn't even see the *Users* menu.
- **Products** with an automatic code (`TUL-0001`, `ROS-BLA-0001`…), categories and a photo. The photo can be kept as a normal picture or converted to **pixel art** (with an option to remove the background). It is always stored in a 256×256 px square.
- **Customers** with validations: Salvadoran phone numbers (8 digits, the dash is added automatically: `1234-5678`), names without numbers, and an optional valid e-mail. Invalid fields show a red warning icon.
- **Invoicing** with a product card menu, customer search, payment method and an editable quantity right in the cart. Two invoice types:
  - **Final consumer:** shows the products and the total (VAT is not mentioned).
  - **Tax credit (crédito fiscal):** shows the products without VAT, the VAT amount and the total.
  - 10 % discount for frequent customers (every 6 purchases over $25, the next one is discounted).
- **Invoice history** with a date filter, line details and a PDF receipt. Invoices are **never deleted, they are voided** (the record and the reason are kept and the stock goes back to the inventory). Each employee only sees the invoices they issued.
- **Stock alerts:** a bell in the top-right corner with a red badge. When a product drops to 10 units or fewer (or runs out) every user gets a notification with date and time, which stays until it is marked as read (the check mark appears when you hover over it). Alerts are also shown again at every login while the stock is still low.
- **Export to Excel and PDF** from any list, styled with the app palette.
- Passwords are stored with a random salt and PBKDF2.

## Running it

1. Install **MySQL 8** and the **.NET 9 SDK**.
2. Run `Database/florehome.sql` in MySQL Workbench.
3. Copy `conexion.example.txt` to `conexion.txt` (or edit the existing one) and set your MySQL user and password. This file is not uploaded to GitHub.
4. Open `FloreHome.sln` in Visual Studio and press **F5**.
5. The first time, the app asks you to create the **administrator** user.
6. If you already have a database from an older version you don't need to do anything: the app adds the new tables and columns on startup.

> The project doesn't generate a `FloreHome.exe` (`UseAppHost=false`): it runs through `dotnet`, so Windows doesn't block an unsigned executable.

## Roles

| | Administrator | Employee |
|---|:---:|:---:|
| Create invoices | ✔ | ✔ |
| View invoices | all | only their own |
| Void invoices | ✔ | |
| Customers: create and edit | ✔ | ✔ |
| Customers: delete | ✔ | |
| Products: view and export | ✔ | ✔ |
| Products: create, edit, delete | ✔ | |
| Users | ✔ | |
| Stock notifications | ✔ | ✔ |

## VAT

Product prices already include VAT (13 %): if a product costs $40, the customer pays $40. The tax rules live in `Core/Fiscal.cs`.

## Project structure

```
Core/       connection, data access, security, validations, VAT rules, product codes, stock alerts
Imaging/    photo → pixel art conversion
Services/   Excel export (ClosedXML) and PDF export (own generator, no dependencies)
Ui/         "Mint chocolate" palette, styles, common controls and the notification bell
Forms/      screens and input dialogs
Database/   SQL script
```

## Palette

Mint chocolate: `#684F48` · `#D6E9ED` · `#CCD6D6` · `#F9E7C7`

## Screenshots

_Coming soon._
