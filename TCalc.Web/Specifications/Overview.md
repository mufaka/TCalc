# TCalc
TCalc is a web based graphing calculator application built using ASP.NET Core MVC. It provides a user-friendly interface for performing various mathematical calculations, including basic arithmetic operations, scientific functions, and more advanced calculations.

## Technologies Used
This will be a responsive web application built using ASP.NET Core Razor Pages. The frontend will be designed using HTML, CSS, and JavaScript to create an intuitive and visually appealing user interface. The backend will be implemented using C# and ASP.NET Core MVC to handle the logic and calculations of the application. Alpine.js and HTMX will be used to enhance the interactivity and responsiveness of the application, allowing for dynamic updates and seamless user interactions without the need for full page reloads. The application will also utilize a graphing library, such as Chart.js or D3.js, to enable graphing capabilities for visualizing mathematical functions and data.

Data persistence will be handled by a **SQLite** database accessed through **Entity Framework Core**. User authentication and account management will be provided by **ASP.NET Core Identity**, allowing users to register, log in, and save their work (data sets, graphs, and calculation history) to the database. **CsvHelper** will be used for robust CSV file parsing when users upload data sets.

## Features

### Responsive Design
The application will be designed to be responsive, ensuring that it works well on various devices and screen sizes. This will allow users to access the calculator from their desktops, laptops, tablets, and smartphones without any issues.

### Dark Mode
The application will include a dark mode option, allowing users to switch between light and dark themes based on their preferences. This will enhance the user experience and provide a visually comfortable interface for users who prefer darker themes.

### Standard Calculator
There will be a standard calculator interface that allows users to perform basic arithmetic operations such as addition, subtraction, multiplication, and division. It will also include buttons for common mathematical functions like square root, exponentiation, and percentage.

### Scientific Calculator
There will be a scientific calculator interface that provides additional functions for more advanced calculations. This will include trigonometric functions (sine, cosine, tangent), logarithmic functions, and other mathematical operations commonly used in scientific calculations.

### Graphing Calculator
There will be a graphing calculator interface that allows users to plot mathematical functions and visualize their graphs. Users can input equations and see the corresponding graphs, which can be useful for understanding the behavior of functions and analyzing their properties.

#### Inequalities
There will be support for graphing inequalities, allowing users to visualize the regions of the graph that satisfy certain conditions. This can be particularly useful for solving systems of inequalities and understanding the relationships between different functions.

#### Transforms
There will be support for graphing transformations of functions, such as translations, reflections, and stretches. Users can input the original function and specify the desired transformations to see how the graph changes accordingly.

#### Conic Graphing
There will be support for graphing conic sections, including circles, ellipses, parabolas, and hyperbolas. Users can input the equations of these conic sections and visualize their graphs to better understand their properties and relationships.

### Geometry Calculator
There will be a geometry calculator interface that allows users to perform calculations related to geometric shapes and figures. This will include functions for calculating areas, perimeters, volumes, and other properties of various geometric shapes such as circles, triangles, rectangles, and more. This will include visual representations of the shapes to help users better understand the calculations and their results.

### Statistics Calculator
There will be a statistics calculator interface that allows users to perform comprehensive statistical analysis on data sets. Users can enter data manually through a friendly spreadsheet-style grid or upload data from CSV files. The calculator will support the following:

#### Descriptive Statistics
Compute mean, median, mode, range, variance, standard deviation, min, max, sum, count, percentiles (Q1, Q2, Q3), interquartile range (IQR), skewness, and kurtosis.

#### Data Visualisation
Generate histograms, box plots, scatter plots, and line charts from user data using Chart.js or D3.js.

#### Regression Analysis
Perform linear regression, polynomial regression, and display the equation, R² value, and trend line on scatter plots.

#### Probability Distributions
Calculate probabilities and visualise common distributions (normal, binomial, Poisson) with user-supplied parameters.

#### Data Entry
Users can enter data values directly into a responsive, spreadsheet-style grid. The grid will support adding/removing rows and columns, naming columns, and inline editing. Data can also be pasted from a clipboard.

#### CSV Upload
Users can upload CSV files to populate data sets. The upload will support header detection, delimiter configuration, and a preview step before importing. Large files will be validated and truncated to a configurable maximum row count.

### User Accounts & Saved Work
The application will support user registration and login via ASP.NET Core Identity backed by a SQLite database. Authenticated users will be able to:
- Save and name data sets for reuse across sessions.
- Save graphing calculator configurations (equations, settings) as named workspaces.
- View a dashboard of their saved items.
- Export saved data sets as CSV files.

Anonymous users can still use all calculator features; data simply will not persist across sessions.

### Database
A SQLite database will be used for all persistent storage. Entity Framework Core will manage the schema through code-first migrations. The database will store:
- ASP.NET Core Identity tables (users, roles, claims, tokens).
- User-owned data sets (metadata + row data).
- Saved graphing workspaces.
- Calculation history (optional, per-user).
