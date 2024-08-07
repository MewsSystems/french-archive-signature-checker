# French archive signature checker

## Mews.Fiscalization.SignatureChecker

This tool can be used to verify the signature of the French fiscal archives. It loads the data that are part of the signature from the ZIP archive and verifies the attached signature using the Mews public key.

### Usage

1. **Extract the ZIP file**:
   - Extract the ZIP file's contents into a folder of your choice.

2. **Navigate to the folder related to your OS (WindowsOS or MacOS)**:
   - Open the extracted folder and navigate to the `WindowsOS` or `MacOS` subfolder.

3. **For Windows, run** `Mews.Fiscalization.SignatureChecker.exe`

### For MacOS
1. **For MacOS Open the terminal (CLI)**:
   - Open the terminal and change the directory to the `MacOS` folder.

2. **Run the following commands in the terminal**:
   - Remove the quarantine attribute:
     ```sh
     xattr -dr com.apple.quarantine Mews.Fiscalization.SignatureChecker
     ```
   - Execute the tool:
     ```sh
     ./Mews.Fiscalization.SignatureChecker
     ```

3. **Handling permission issues**:
   - If you encounter any permission issues, run the following command to make the tool executable:
     ```sh
     chmod +x Mews.Fiscalization.SignatureChecker
     ```
   - Then, execute the tool again:
     ```sh
     ./Mews.Fiscalization.SignatureChecker
     ```
     
After finishing the steps above, run the following command to verify the signature of an archive zip file:  ```2025.zip --production``` or ```2025.zip --develop```

### Parameters

- `path-to-archive` - Path to the ZIP archive that contains the signature and the data that are part of the signature.
- `environment` - Environment for which the signature should be verified. Possible values are `production` and `develop`, if not provided, `production` environment will be used.

### Output

The tool will output the result of the signature verification. If the signature is valid, the tool will output `Archive signature is valid.`. If the signature is invalid, the tool will output `Archive signature is invalid.`.
