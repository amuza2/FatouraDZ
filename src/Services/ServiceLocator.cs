namespace FatouraDZ.Services;

public static class ServiceLocator
{
    private static IDatabaseService? _databaseService;
    private static ICalculationService? _calculationService;
    private static IInvoiceNumberService? _invoiceNumberService;
    private static INumberToWordsService? _numberToWordsService;
    private static IPdfService? _pdfService;
    private static IExcelService? _excelService;
    private static IValidationService? _validationService;

    public static IDatabaseService DatabaseService => 
        _databaseService ??= new DatabaseService();

    public static ICalculationService CalculationService => 
        _calculationService ??= new CalculationService();

    public static IInvoiceNumberService InvoiceNumberService => 
        _invoiceNumberService ??= new InvoiceNumberService(DatabaseService);

    public static INumberToWordsService NumberToWordsService => 
        _numberToWordsService ??= new NumberToWordsService();

    public static IPdfService PdfService => 
        _pdfService ??= new PdfService(NumberToWordsService);

    public static IExcelService ExcelService => 
        _excelService ??= new ExcelService(NumberToWordsService);

    public static IValidationService ValidationService => 
        _validationService ??= new ValidationService();
}
