# Documents Generator
This document generator allows for template and document generation.
## How it works
This program is made of three main components: Template Generation, Data Sheet Management and Document Generation.
### Template Generation
This component takes one or more `.docx` files, detects placeholder tags like `{date}`, `{city name}`, etc... directly in the document text and exports them to an `.xml` file. Next, the document is rewritten so each tag becomes a Word content control bound to that XML.
For every document, the program returns two files: `.xml` "data sheet" with keys and default values (of key names) and  `.docx` "template" with tags bound to the data sheet. In addition, the program returns merged XML file from all generated data sheets, without duplicates. The generated documents are saved to a desired directory in two subdirectories.
### Data Sheet Management
This component allows to edit a data sheet or create a new merged one from selected `.xml` files. The generated data sheet is saved to a desired directory.
### Document Generation
This component takes one or more generated templates and a data sheet. The XML data in given templates is swapped with the new one, without losing formatting. The generated documents are saved to a desired directory.
## Author
Artur Życzyński
## License
MIT
