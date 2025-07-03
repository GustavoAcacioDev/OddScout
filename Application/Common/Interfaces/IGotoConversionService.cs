namespace OddScout.Application.Common.Interfaces;

public interface IGotoConversionService
{
    decimal[] ConvertOddsToProbabilities(decimal[] odds);

    (decimal[] gotoConversion, decimal[] simpleNormalization) CompareConversionMethods(decimal[] odds);
}