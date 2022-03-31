using System;

namespace OverScript
{



    public partial class BasicFunctions
    {

        private static int AdditionOp_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack) + fnArgs[1].EvalInt(scope, inst, cstack);
        private static int SubtractionOp_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack) - fnArgs[1].EvalInt(scope, inst, cstack);
        private static int MultiplyOp_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack) * fnArgs[1].EvalInt(scope, inst, cstack);
        private static int DivisionOp_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack) / fnArgs[1].EvalInt(scope, inst, cstack);
        private static int ModulusOp_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack) % fnArgs[1].EvalInt(scope, inst, cstack);
        private static int BitwiseAndOp_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack) & fnArgs[1].EvalInt(scope, inst, cstack);
        private static int BitwiseOrOp_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack) | fnArgs[1].EvalInt(scope, inst, cstack);
        private static int ExclusiveOrOp_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack) ^ fnArgs[1].EvalInt(scope, inst, cstack);
        private static int LeftShiftOp_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack) << fnArgs[1].EvalInt(scope, inst, cstack);
        private static int RightShiftOp_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack) >> fnArgs[1].EvalInt(scope, inst, cstack);
        private static int OnesComplementOp_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ~fnArgs[1].EvalInt(scope, inst, cstack);
        private static bool EqualityOp_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack) == fnArgs[1].EvalInt(scope, inst, cstack);
        private static bool InequalityOp_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack) != fnArgs[1].EvalInt(scope, inst, cstack);
        private static bool LogicalNotOp_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[1].EvalInt(scope, inst, cstack) == 0;
        private static bool GreaterThanOp_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack) > fnArgs[1].EvalInt(scope, inst, cstack);
        private static bool GreaterThanOrEqualOp_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack) >= fnArgs[1].EvalInt(scope, inst, cstack);
        private static bool LessThanOp_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack) < fnArgs[1].EvalInt(scope, inst, cstack);
        private static bool LessThanOrEqualOp_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack) <= fnArgs[1].EvalInt(scope, inst, cstack);
        private static bool LogicalAndOp_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack) != 0 && fnArgs[1].EvalInt(scope, inst, cstack) != 0;
        private static bool LogicalOrOp_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack) != 0 || fnArgs[1].EvalInt(scope, inst, cstack) != 0;
        private static int AdditionOp_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack) + fnArgs[1].EvalByte(scope, inst, cstack);
        private static int SubtractionOp_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack) - fnArgs[1].EvalByte(scope, inst, cstack);
        private static int MultiplyOp_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack) * fnArgs[1].EvalByte(scope, inst, cstack);
        private static int DivisionOp_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack) / fnArgs[1].EvalByte(scope, inst, cstack);
        private static int ModulusOp_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack) % fnArgs[1].EvalByte(scope, inst, cstack);
        private static int BitwiseAndOp_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack) & fnArgs[1].EvalByte(scope, inst, cstack);
        private static int BitwiseOrOp_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack) | fnArgs[1].EvalByte(scope, inst, cstack);
        private static int ExclusiveOrOp_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack) ^ fnArgs[1].EvalByte(scope, inst, cstack);
        private static int LeftShiftOp_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack) << fnArgs[1].EvalByte(scope, inst, cstack);
        private static int RightShiftOp_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack) >> fnArgs[1].EvalByte(scope, inst, cstack);
        private static int OnesComplementOp_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ~fnArgs[1].EvalByte(scope, inst, cstack);
        private static bool EqualityOp_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack) == fnArgs[1].EvalByte(scope, inst, cstack);
        private static bool InequalityOp_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack) != fnArgs[1].EvalByte(scope, inst, cstack);
        private static bool LogicalNotOp_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[1].EvalByte(scope, inst, cstack) == 0;
        private static bool GreaterThanOp_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack) > fnArgs[1].EvalByte(scope, inst, cstack);
        private static bool GreaterThanOrEqualOp_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack) >= fnArgs[1].EvalByte(scope, inst, cstack);
        private static bool LessThanOp_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack) < fnArgs[1].EvalByte(scope, inst, cstack);
        private static bool LessThanOrEqualOp_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack) <= fnArgs[1].EvalByte(scope, inst, cstack);
        private static bool LogicalAndOp_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack) != 0 && fnArgs[1].EvalByte(scope, inst, cstack) != 0;
        private static bool LogicalOrOp_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack) != 0 || fnArgs[1].EvalByte(scope, inst, cstack) != 0;

        private static int AdditionOp_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack) + fnArgs[1].EvalShort(scope, inst, cstack);
        private static int SubtractionOp_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack) - fnArgs[1].EvalShort(scope, inst, cstack);
        private static int MultiplyOp_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack) * fnArgs[1].EvalShort(scope, inst, cstack);
        private static int DivisionOp_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack) / fnArgs[1].EvalShort(scope, inst, cstack);
        private static int ModulusOp_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack) % fnArgs[1].EvalShort(scope, inst, cstack);
        private static int BitwiseAndOp_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack) & fnArgs[1].EvalShort(scope, inst, cstack);
        private static int BitwiseOrOp_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack) | fnArgs[1].EvalShort(scope, inst, cstack);
        private static int ExclusiveOrOp_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack) ^ fnArgs[1].EvalShort(scope, inst, cstack);
        private static int LeftShiftOp_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack) << fnArgs[1].EvalShort(scope, inst, cstack);
        private static int RightShiftOp_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack) >> fnArgs[1].EvalShort(scope, inst, cstack);
        private static int OnesComplementOp_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ~fnArgs[1].EvalShort(scope, inst, cstack);
        private static bool EqualityOp_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack) == fnArgs[1].EvalShort(scope, inst, cstack);
        private static bool InequalityOp_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack) != fnArgs[1].EvalShort(scope, inst, cstack);
        private static bool LogicalNotOp_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[1].EvalShort(scope, inst, cstack) == 0;
        private static bool GreaterThanOp_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack) > fnArgs[1].EvalShort(scope, inst, cstack);
        private static bool GreaterThanOrEqualOp_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack) >= fnArgs[1].EvalShort(scope, inst, cstack);
        private static bool LessThanOp_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack) < fnArgs[1].EvalShort(scope, inst, cstack);
        private static bool LessThanOrEqualOp_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack) <= fnArgs[1].EvalShort(scope, inst, cstack);
        private static bool LogicalAndOp_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack) != 0 && fnArgs[1].EvalShort(scope, inst, cstack) != 0;
        private static bool LogicalOrOp_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack) != 0 || fnArgs[1].EvalShort(scope, inst, cstack) != 0;

        private static long AdditionOp_long_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack) + fnArgs[1].EvalLong(scope, inst, cstack);
        private static long SubtractionOp_long_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack) - fnArgs[1].EvalLong(scope, inst, cstack);
        private static long MultiplyOp_long_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack) * fnArgs[1].EvalLong(scope, inst, cstack);
        private static long DivisionOp_long_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack) / fnArgs[1].EvalLong(scope, inst, cstack);
        private static long ModulusOp_long_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack) % fnArgs[1].EvalLong(scope, inst, cstack);
        private static long BitwiseAndOp_long_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack) & fnArgs[1].EvalLong(scope, inst, cstack);
        private static long BitwiseOrOp_long_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack) | fnArgs[1].EvalLong(scope, inst, cstack);
        private static long ExclusiveOrOp_long_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack) ^ fnArgs[1].EvalLong(scope, inst, cstack);
        private static long OnesComplementOp_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ~fnArgs[1].EvalLong(scope, inst, cstack);
        private static bool EqualityOp_long_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack) == fnArgs[1].EvalLong(scope, inst, cstack);
        private static bool InequalityOp_long_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack) != fnArgs[1].EvalLong(scope, inst, cstack);
        private static bool LogicalNotOp_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[1].EvalLong(scope, inst, cstack) == 0;
        private static bool GreaterThanOp_long_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack) > fnArgs[1].EvalLong(scope, inst, cstack);
        private static bool GreaterThanOrEqualOp_long_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack) >= fnArgs[1].EvalLong(scope, inst, cstack);
        private static bool LessThanOp_long_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack) < fnArgs[1].EvalLong(scope, inst, cstack);
        private static bool LessThanOrEqualOp_long_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack) <= fnArgs[1].EvalLong(scope, inst, cstack);
        private static bool LogicalAndOp_long_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack) != 0 && fnArgs[1].EvalLong(scope, inst, cstack) != 0;
        private static bool LogicalOrOp_long_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack) != 0 || fnArgs[1].EvalLong(scope, inst, cstack) != 0;
        private static float AdditionOp_float_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalFloat(scope, inst, cstack) + fnArgs[1].EvalFloat(scope, inst, cstack);
        private static float SubtractionOp_float_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalFloat(scope, inst, cstack) - fnArgs[1].EvalFloat(scope, inst, cstack);
        private static float MultiplyOp_float_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalFloat(scope, inst, cstack) * fnArgs[1].EvalFloat(scope, inst, cstack);
        private static float DivisionOp_float_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalFloat(scope, inst, cstack) / fnArgs[1].EvalFloat(scope, inst, cstack);
        private static float ModulusOp_float_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalFloat(scope, inst, cstack) % fnArgs[1].EvalFloat(scope, inst, cstack);
        private static bool EqualityOp_float_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalFloat(scope, inst, cstack) == fnArgs[1].EvalFloat(scope, inst, cstack);
        private static bool InequalityOp_float_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalFloat(scope, inst, cstack) != fnArgs[1].EvalFloat(scope, inst, cstack);
        private static bool LogicalNotOp_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[1].EvalFloat(scope, inst, cstack) == 0;
        private static bool GreaterThanOp_float_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalFloat(scope, inst, cstack) > fnArgs[1].EvalFloat(scope, inst, cstack);
        private static bool GreaterThanOrEqualOp_float_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalFloat(scope, inst, cstack) >= fnArgs[1].EvalFloat(scope, inst, cstack);
        private static bool LessThanOp_float_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalFloat(scope, inst, cstack) < fnArgs[1].EvalFloat(scope, inst, cstack);
        private static bool LessThanOrEqualOp_float_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalFloat(scope, inst, cstack) <= fnArgs[1].EvalFloat(scope, inst, cstack);
        private static bool LogicalAndOp_float_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalFloat(scope, inst, cstack) != 0 && fnArgs[1].EvalFloat(scope, inst, cstack) != 0;
        private static bool LogicalOrOp_float_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalFloat(scope, inst, cstack) != 0 || fnArgs[1].EvalFloat(scope, inst, cstack) != 0;
        private static double AdditionOp_double_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDouble(scope, inst, cstack) + fnArgs[1].EvalDouble(scope, inst, cstack);
        private static double SubtractionOp_double_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDouble(scope, inst, cstack) - fnArgs[1].EvalDouble(scope, inst, cstack);
        private static double MultiplyOp_double_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDouble(scope, inst, cstack) * fnArgs[1].EvalDouble(scope, inst, cstack);
        private static double DivisionOp_double_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDouble(scope, inst, cstack) / fnArgs[1].EvalDouble(scope, inst, cstack);
        private static double ModulusOp_double_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDouble(scope, inst, cstack) % fnArgs[1].EvalDouble(scope, inst, cstack);
        private static bool EqualityOp_double_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDouble(scope, inst, cstack) == fnArgs[1].EvalDouble(scope, inst, cstack);
        private static bool InequalityOp_double_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDouble(scope, inst, cstack) != fnArgs[1].EvalDouble(scope, inst, cstack);
        private static bool LogicalNotOp_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[1].EvalDouble(scope, inst, cstack) == 0;
        private static bool GreaterThanOp_double_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDouble(scope, inst, cstack) > fnArgs[1].EvalDouble(scope, inst, cstack);
        private static bool GreaterThanOrEqualOp_double_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDouble(scope, inst, cstack) >= fnArgs[1].EvalDouble(scope, inst, cstack);
        private static bool LessThanOp_double_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDouble(scope, inst, cstack) < fnArgs[1].EvalDouble(scope, inst, cstack);
        private static bool LessThanOrEqualOp_double_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDouble(scope, inst, cstack) <= fnArgs[1].EvalDouble(scope, inst, cstack);
        private static bool LogicalAndOp_double_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDouble(scope, inst, cstack) != 0 && fnArgs[1].EvalDouble(scope, inst, cstack) != 0;
        private static bool LogicalOrOp_double_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDouble(scope, inst, cstack) != 0 || fnArgs[1].EvalDouble(scope, inst, cstack) != 0;
        private static decimal AdditionOp_decimal_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDecimal(scope, inst, cstack) + fnArgs[1].EvalDecimal(scope, inst, cstack);
        private static decimal SubtractionOp_decimal_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDecimal(scope, inst, cstack) - fnArgs[1].EvalDecimal(scope, inst, cstack);
        private static decimal MultiplyOp_decimal_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDecimal(scope, inst, cstack) * fnArgs[1].EvalDecimal(scope, inst, cstack);
        private static decimal DivisionOp_decimal_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDecimal(scope, inst, cstack) / fnArgs[1].EvalDecimal(scope, inst, cstack);
        private static decimal ModulusOp_decimal_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDecimal(scope, inst, cstack) % fnArgs[1].EvalDecimal(scope, inst, cstack);
        private static bool EqualityOp_decimal_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDecimal(scope, inst, cstack) == fnArgs[1].EvalDecimal(scope, inst, cstack);
        private static bool InequalityOp_decimal_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDecimal(scope, inst, cstack) != fnArgs[1].EvalDecimal(scope, inst, cstack);
        private static bool LogicalNotOp_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[1].EvalDecimal(scope, inst, cstack) == 0;
        private static bool GreaterThanOp_decimal_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDecimal(scope, inst, cstack) > fnArgs[1].EvalDecimal(scope, inst, cstack);
        private static bool GreaterThanOrEqualOp_decimal_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDecimal(scope, inst, cstack) >= fnArgs[1].EvalDecimal(scope, inst, cstack);
        private static bool LessThanOp_decimal_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDecimal(scope, inst, cstack) < fnArgs[1].EvalDecimal(scope, inst, cstack);
        private static bool LessThanOrEqualOp_decimal_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDecimal(scope, inst, cstack) <= fnArgs[1].EvalDecimal(scope, inst, cstack);
        private static bool LogicalAndOp_decimal_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDecimal(scope, inst, cstack) != 0 && fnArgs[1].EvalDecimal(scope, inst, cstack) != 0;
        private static bool LogicalOrOp_decimal_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDecimal(scope, inst, cstack) != 0 || fnArgs[1].EvalDecimal(scope, inst, cstack) != 0;
        private static int AdditionOp_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack) + fnArgs[1].EvalChar(scope, inst, cstack);
        private static int SubtractionOp_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack) - fnArgs[1].EvalChar(scope, inst, cstack);
        private static int MultiplyOp_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack) * fnArgs[1].EvalChar(scope, inst, cstack);
        private static int DivisionOp_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack) / fnArgs[1].EvalChar(scope, inst, cstack);
        private static int ModulusOp_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack) % fnArgs[1].EvalChar(scope, inst, cstack);
        private static int BitwiseAndOp_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack) & fnArgs[1].EvalChar(scope, inst, cstack);
        private static int BitwiseOrOp_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack) | fnArgs[1].EvalChar(scope, inst, cstack);
        private static int ExclusiveOrOp_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack) ^ fnArgs[1].EvalChar(scope, inst, cstack);
        private static int LeftShiftOp_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack) << fnArgs[1].EvalChar(scope, inst, cstack);
        private static int RightShiftOp_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack) >> fnArgs[1].EvalChar(scope, inst, cstack);
        private static int OnesComplementOp_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ~fnArgs[1].EvalChar(scope, inst, cstack);
        private static bool EqualityOp_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack) == fnArgs[1].EvalChar(scope, inst, cstack);
        private static bool InequalityOp_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack) != fnArgs[1].EvalChar(scope, inst, cstack);
        private static bool LogicalNotOp_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[1].EvalChar(scope, inst, cstack) == 0;
        private static bool GreaterThanOp_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack) > fnArgs[1].EvalChar(scope, inst, cstack);
        private static bool GreaterThanOrEqualOp_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack) >= fnArgs[1].EvalChar(scope, inst, cstack);
        private static bool LessThanOp_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack) < fnArgs[1].EvalChar(scope, inst, cstack);
        private static bool LessThanOrEqualOp_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack) <= fnArgs[1].EvalChar(scope, inst, cstack);
        private static bool LogicalAndOp_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack) != 0 && fnArgs[1].EvalChar(scope, inst, cstack) != 0;
        private static bool LogicalOrOp_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack) != 0 || fnArgs[1].EvalChar(scope, inst, cstack) != 0;
        private static bool EqualityOp_date_date(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDate(scope, inst, cstack) == fnArgs[1].EvalDate(scope, inst, cstack);
        private static bool InequalityOp_date_date(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDate(scope, inst, cstack) != fnArgs[1].EvalDate(scope, inst, cstack);
        private static bool LogicalNotOp_date(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[1].EvalDate(scope, inst, cstack) == default(DateTime);
        private static bool GreaterThanOp_date_date(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDate(scope, inst, cstack) > fnArgs[1].EvalDate(scope, inst, cstack);
        private static bool GreaterThanOrEqualOp_date_date(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDate(scope, inst, cstack) >= fnArgs[1].EvalDate(scope, inst, cstack);
        private static bool LessThanOp_date_date(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDate(scope, inst, cstack) < fnArgs[1].EvalDate(scope, inst, cstack);
        private static bool LessThanOrEqualOp_date_date(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDate(scope, inst, cstack) <= fnArgs[1].EvalDate(scope, inst, cstack);
        private static bool LogicalAndOp_date_date(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDate(scope, inst, cstack) != default(DateTime) && fnArgs[1].EvalDate(scope, inst, cstack) != default(DateTime);
        private static bool LogicalOrOp_date_date(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDate(scope, inst, cstack) != default(DateTime) || fnArgs[1].EvalDate(scope, inst, cstack) != default(DateTime);
        private static string AdditionOp_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack) + fnArgs[1].EvalString(scope, inst, cstack);
        private static bool EqualityOp_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack) == fnArgs[1].EvalString(scope, inst, cstack);
        private static bool InequalityOp_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack) != fnArgs[1].EvalString(scope, inst, cstack);
        private static bool LogicalNotOp_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => String.IsNullOrEmpty(fnArgs[1].EvalString(scope, inst, cstack));
        private static bool GreaterThanOp_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).CompareTo(fnArgs[1].EvalString(scope, inst, cstack)) > 0;
        private static bool GreaterThanOrEqualOp_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).CompareTo(fnArgs[1].EvalString(scope, inst, cstack)) >= 0;
        private static bool LessThanOp_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).CompareTo(fnArgs[1].EvalString(scope, inst, cstack)) < 0;
        private static bool LessThanOrEqualOp_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).CompareTo(fnArgs[1].EvalString(scope, inst, cstack)) <= 0;
        private static bool LogicalAndOp_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => String.IsNullOrEmpty(fnArgs[0].EvalString(scope, inst, cstack)) && String.IsNullOrEmpty(fnArgs[1].EvalString(scope, inst, cstack));
        private static bool LogicalOrOp_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => String.IsNullOrEmpty(fnArgs[0].EvalString(scope, inst, cstack)) || String.IsNullOrEmpty(fnArgs[1].EvalString(scope, inst, cstack));

        private static bool BitwiseAndOp_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) & fnArgs[1].EvalBool(scope, inst, cstack);
        private static bool BitwiseOrOp_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) | fnArgs[1].EvalBool(scope, inst, cstack);
        private static bool ExclusiveOrOp_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ^ fnArgs[1].EvalBool(scope, inst, cstack);

        private static bool EqualityOp_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) == fnArgs[1].EvalBool(scope, inst, cstack);
        private static bool InequalityOp_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) != fnArgs[1].EvalBool(scope, inst, cstack);
        private static bool LogicalNotOp_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => !fnArgs[1].EvalBool(scope, inst, cstack);
        private static bool GreaterThanOp_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack).CompareTo(fnArgs[1].EvalBool(scope, inst, cstack)) > 0;
        private static bool GreaterThanOrEqualOp_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack).CompareTo(fnArgs[1].EvalBool(scope, inst, cstack)) >= 0;
        private static bool LessThanOp_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack).CompareTo(fnArgs[1].EvalBool(scope, inst, cstack)) < 0;
        private static bool LessThanOrEqualOp_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack).CompareTo(fnArgs[1].EvalBool(scope, inst, cstack)) <= 0;
        private static bool LogicalAndOp_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) && fnArgs[1].EvalBool(scope, inst, cstack);
        private static bool LogicalOrOp_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) || fnArgs[1].EvalBool(scope, inst, cstack);



        private static bool EqualityOp_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalObject(scope, inst, cstack) == fnArgs[1].EvalObject(scope, inst, cstack);
        private static bool EqualityOp_custom_custom(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var a = fnArgs[0].EvalCustom(scope, inst, cstack);
            var b = fnArgs[1].EvalCustom(scope, inst, cstack);

            return (a == null ? null : a.Object) == (b == null ? null : b.Object);
        }
        private static bool InequalityOp_custom_custom(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var a = fnArgs[0].EvalCustom(scope, inst, cstack);
            var b = fnArgs[1].EvalCustom(scope, inst, cstack);

            return (a == null ? null : a.Object) != (b == null ? null : b.Object);
        }

        private static bool InequalityOp_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalObject(scope, inst, cstack) != fnArgs[1].EvalObject(scope, inst, cstack);

        private static byte ByteCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (byte)fnArgs[0].EvalObject(scope, inst, cstack);
        private static byte ByteCastingOp_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (byte)fnArgs[0].EvalChar(scope, inst, cstack);
        private static byte ByteCastingOp_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (byte)fnArgs[0].EvalShort(scope, inst, cstack);
        private static byte ByteCastingOp_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (byte)fnArgs[0].EvalInt(scope, inst, cstack);
        private static byte ByteCastingOp_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (byte)fnArgs[0].EvalLong(scope, inst, cstack);
        private static byte ByteCastingOp_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (byte)fnArgs[0].EvalFloat(scope, inst, cstack);
        private static byte ByteCastingOp_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (byte)fnArgs[0].EvalDouble(scope, inst, cstack);
        private static byte ByteCastingOp_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (byte)fnArgs[0].EvalDecimal(scope, inst, cstack);

        private static short ShortCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (short)fnArgs[0].EvalObject(scope, inst, cstack);
        private static short ShortCastingOp_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (short)fnArgs[0].EvalChar(scope, inst, cstack);
        private static short ShortCastingOp_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack);
        private static short ShortCastingOp_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (short)fnArgs[0].EvalInt(scope, inst, cstack);
        private static short ShortCastingOp_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (short)fnArgs[0].EvalLong(scope, inst, cstack);
        private static short ShortCastingOp_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (short)fnArgs[0].EvalFloat(scope, inst, cstack);
        private static short ShortCastingOp_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (short)fnArgs[0].EvalDouble(scope, inst, cstack);
        private static short ShortCastingOp_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (short)fnArgs[0].EvalDecimal(scope, inst, cstack);

        private static int IntCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (int)fnArgs[0].EvalObject(scope, inst, cstack);
        private static int IntCastingOp_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack);
        private static int IntCastingOp_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack);
        private static int IntCastingOp_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack);
        private static int IntCastingOp_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (int)fnArgs[0].EvalLong(scope, inst, cstack);
        private static int IntCastingOp_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (int)fnArgs[0].EvalFloat(scope, inst, cstack);
        private static int IntCastingOp_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (int)fnArgs[0].EvalDouble(scope, inst, cstack);
        private static int IntCastingOp_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (int)fnArgs[0].EvalDecimal(scope, inst, cstack);

        private static long LongCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (long)fnArgs[0].EvalObject(scope, inst, cstack);
        private static long LongCastingOp_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack);
        private static long LongCastingOp_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack);
        private static long LongCastingOp_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack);
        private static long LongCastingOp_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack);
        private static long LongCastingOp_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (long)fnArgs[0].EvalFloat(scope, inst, cstack);
        private static long LongCastingOp_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (long)fnArgs[0].EvalDouble(scope, inst, cstack);
        private static long LongCastingOp_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (long)fnArgs[0].EvalDecimal(scope, inst, cstack);

        private static float FloatCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (float)fnArgs[0].EvalObject(scope, inst, cstack);
        private static float FloatCastingOp_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack);
        private static float FloatCastingOp_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack);
        private static float FloatCastingOp_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack);
        private static float FloatCastingOp_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack);
        private static float FloatCastingOp_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack);
        private static float FloatCastingOp_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (float)fnArgs[0].EvalDouble(scope, inst, cstack);
        private static float FloatCastingOp_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (float)fnArgs[0].EvalDecimal(scope, inst, cstack);

        private static double DoubleCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (double)fnArgs[0].EvalObject(scope, inst, cstack);
        private static double DoubleCastingOp_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack);
        private static double DoubleCastingOp_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack);
        private static double DoubleCastingOp_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack);
        private static double DoubleCastingOp_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack);
        private static double DoubleCastingOp_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack);
        private static double DoubleCastingOp_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalFloat(scope, inst, cstack);
        private static double DoubleCastingOp_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (double)fnArgs[0].EvalDecimal(scope, inst, cstack);

        private static decimal DecimalCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (decimal)fnArgs[0].EvalObject(scope, inst, cstack);
        private static decimal DecimalCastingOp_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack);
        private static decimal DecimalCastingOp_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack);
        private static decimal DecimalCastingOp_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack);
        private static decimal DecimalCastingOp_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack);
        private static decimal DecimalCastingOp_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack);
        private static decimal DecimalCastingOp_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (decimal)fnArgs[0].EvalFloat(scope, inst, cstack);
        private static decimal DecimalCastingOp_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (decimal)fnArgs[0].EvalDouble(scope, inst, cstack);

        private static char CharCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (char)fnArgs[0].EvalObject(scope, inst, cstack);
        private static char CharCastingOp_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (char)fnArgs[0].EvalDecimal(scope, inst, cstack);
        private static char CharCastingOp_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (char)fnArgs[0].EvalByte(scope, inst, cstack);
        private static char CharCastingOp_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (char)fnArgs[0].EvalShort(scope, inst, cstack);
        private static char CharCastingOp_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (char)fnArgs[0].EvalInt(scope, inst, cstack);
        private static char CharCastingOp_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (char)fnArgs[0].EvalLong(scope, inst, cstack);
        private static char CharCastingOp_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (char)fnArgs[0].EvalFloat(scope, inst, cstack);
        private static char CharCastingOp_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (char)fnArgs[0].EvalDouble(scope, inst, cstack);



        private static bool BoolCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (bool)fnArgs[0].EvalObject(scope, inst, cstack);
        private static string StringCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (string)fnArgs[0].EvalObject(scope, inst, cstack);



        private static DateTime DateCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (DateTime)fnArgs[0].EvalObject(scope, inst, cstack);

        private static int[] IntArrayCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (int[])fnArgs[0].EvalObject(scope, inst, cstack);
        private static bool[] BoolArrayCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (bool[])fnArgs[0].EvalObject(scope, inst, cstack);
        private static string[] StringArrayCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (string[])fnArgs[0].EvalObject(scope, inst, cstack);
        private static char[] CharArrayCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (char[])fnArgs[0].EvalObject(scope, inst, cstack);
        private static decimal[] DecimalArrayCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (decimal[])fnArgs[0].EvalObject(scope, inst, cstack);
        private static long[] LongArrayCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (long[])fnArgs[0].EvalObject(scope, inst, cstack);
        private static object[] ObjectArrayCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (object[])fnArgs[0].EvalObject(scope, inst, cstack);
        private static double[] DoubleArrayCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (double[])fnArgs[0].EvalObject(scope, inst, cstack);
        private static float[] FloatArrayCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (float[])fnArgs[0].EvalObject(scope, inst, cstack);
        private static byte[] ByteArrayCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (byte[])fnArgs[0].EvalObject(scope, inst, cstack);
        private static short[] ShortArrayCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (short[])fnArgs[0].EvalObject(scope, inst, cstack);
        private static DateTime[] DateArrayCastingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (DateTime[])fnArgs[0].EvalObject(scope, inst, cstack);



        private static object BoxingOp_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalObject(scope, inst, cstack);


        private static object ByHintCastingOp_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Casting(fnArgs[0].EvalObject(scope, inst, cstack), fnArgs[1].Type.SubType);
        private static object ByHintConvCastingOp_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ConvCasting(fnArgs[0].EvalObject(scope, inst, cstack), fnArgs[1].Type.SubType);

        private static object Casting(object obj, Type type)
        {
            if (type.IsInstanceOfType(obj)) return obj;
            throw new InvalidCastException($"Unable to cast object of type '{obj.GetType()}' to type '{type}'.");
        }
        private static object ConvCasting(object obj, Type type)
        {
            if (type.IsInstanceOfType(obj)) return obj;
            return Convert.ChangeType(obj, type);
        }
        private static object[] ByHintArrayCastingOp_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {

            object obj = fnArgs[0].EvalObject(scope, inst, cstack);
            var type = fnArgs[1].Type.SubType;
            if (obj is object[] objArr)
            {
                var elemType = type.GetElementType();

                int n = FindMismatchedElement(objArr, elemType);
                if (n >= 0) throw new ScriptExecutionException($"Failed to cast because element at index {n} is not of type '{elemType.FullName}'.");


                return objArr;
            }



            else
                throw new ScriptExecutionException("Failed to cast because object is not of type 'object[]'.");


        }


        private static int FindMismatchedElement(object[] arr, Type elemType)
        {
            bool isRefType = !elemType.IsValueType;
            int n = -1;
            foreach (var item in arr)
            {
                n++;
                if (!((item == null && isRefType) || elemType.IsInstanceOfType(item))) return n;
            }
            return -1;
        }
        private static int IncrementOp_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return fnArgs[0].EvalInt(scope, inst, cstack) + 1;
        }
        private static byte IncrementOp_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return (byte)(fnArgs[0].EvalByte(scope, inst, cstack) + 1);
        }
        private static short IncrementOp_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return (short)(fnArgs[0].EvalShort(scope, inst, cstack) + 1);
        }
        private static long IncrementOp_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return fnArgs[0].EvalLong(scope, inst, cstack) + 1;
        }
        private static float IncrementOp_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return fnArgs[0].EvalFloat(scope, inst, cstack) + 1;
        }
        private static double IncrementOp_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return fnArgs[0].EvalDouble(scope, inst, cstack) + 1;
        }
        private static decimal IncrementOp_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return fnArgs[0].EvalDecimal(scope, inst, cstack) + 1;
        }
        private static char IncrementOp_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return (char)(fnArgs[0].EvalChar(scope, inst, cstack) + 1);
        }

        private static int DecrementOp_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return fnArgs[0].EvalInt(scope, inst, cstack) - 1;
        }
        private static byte DecrementOp_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return (byte)(fnArgs[0].EvalByte(scope, inst, cstack) - 1);
        }
        private static short DecrementOp_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return (short)(fnArgs[0].EvalShort(scope, inst, cstack) - 1);
        }
        private static long DecrementOp_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return fnArgs[0].EvalLong(scope, inst, cstack) - 1;
        }
        private static float DecrementOp_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return fnArgs[0].EvalFloat(scope, inst, cstack) - 1;
        }
        private static double DecrementOp_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return fnArgs[0].EvalDouble(scope, inst, cstack) - 1;
        }
        private static decimal DecrementOp_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return fnArgs[0].EvalDecimal(scope, inst, cstack) - 1;
        }
        private static char DecrementOp_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return (char)(fnArgs[0].EvalChar(scope, inst, cstack) - 1);
        }
        private static CustomObject CustomCastingOp_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var objArg = fnArgs[1];
            var typeArg = fnArgs[0];
            CustomObject co = null;
            if (objArg.Type.ID == TypeID.Object)
            {
                object obj = objArg.EvalObject(scope, inst, cstack);

                if (obj != null)
                {
                    co = obj as CustomObject;
                    if (co == null) throw new ScriptExecutionException($"Unable to cast object of type '{obj.GetType()}' to type '{typeArg.Type.CType.FullName}'.");
                    else if (co.Type.IsArray != typeArg.Type.CType.IsArray || !co.Type.Class.Is(typeArg.Type.CType.Class)) throw new ScriptExecutionException($"Unable to cast object of type '{co.Type.FullName}' to type '{typeArg.Type.CType.FullName}'.");
                }
            }
            else
                co = objArg.EvalCustom(scope, inst, cstack);

            return co;
        }

        private static object CoalescingOp_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            object obj = fnArgs[0].EvalObject(scope, inst, cstack);
            if (obj != null) return obj;
            return fnArgs[1].EvalObject(scope, inst, cstack);
        }
        private static string CoalescingOp_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack) ?? fnArgs[1].EvalString(scope, inst, cstack);
        private static CustomObject CoalescingOp_custom_custom(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalCustom(scope, inst, cstack) ?? fnArgs[1].EvalCustom(scope, inst, cstack);
        private static CustomObject CoalescingOp_customArray_customArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalCustomArray(scope, inst, cstack) ?? fnArgs[1].EvalCustomArray(scope, inst, cstack);

        private static int[] CoalescingOp_intArray_intArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalIntArray(scope, inst, cstack) ?? fnArgs[1].EvalIntArray(scope, inst, cstack);
        private static long[] CoalescingOp_longArray_longArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLongArray(scope, inst, cstack) ?? fnArgs[1].EvalLongArray(scope, inst, cstack);
        private static float[] CoalescingOp_floatArray_floatArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalFloatArray(scope, inst, cstack) ?? fnArgs[1].EvalFloatArray(scope, inst, cstack);
        private static double[] CoalescingOp_doubleArray_doubleArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDoubleArray(scope, inst, cstack) ?? fnArgs[1].EvalDoubleArray(scope, inst, cstack);
        private static decimal[] CoalescingOp_decimalArray_decimalArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDecimalArray(scope, inst, cstack) ?? fnArgs[1].EvalDecimalArray(scope, inst, cstack);
        private static bool[] CoalescingOp_boolArray_boolArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBoolArray(scope, inst, cstack) ?? fnArgs[1].EvalBoolArray(scope, inst, cstack);
        private static string[] CoalescingOp_stringArray_stringArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalStringArray(scope, inst, cstack) ?? fnArgs[1].EvalStringArray(scope, inst, cstack);
        private static char[] CoalescingOp_charArray_charArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalCharArray(scope, inst, cstack) ?? fnArgs[1].EvalCharArray(scope, inst, cstack);
        private static byte[] CoalescingOp_byteArray_byteArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByteArray(scope, inst, cstack) ?? fnArgs[1].EvalByteArray(scope, inst, cstack);
        private static short[] CoalescingOp_shortArray_shortArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShortArray(scope, inst, cstack) ?? fnArgs[1].EvalShortArray(scope, inst, cstack);
        private static DateTime[] CoalescingOp_dateArray_dateArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDateArray(scope, inst, cstack) ?? fnArgs[1].EvalDateArray(scope, inst, cstack);
        private static object[] CoalescingOp_objectArray_objectArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalObjectArray(scope, inst, cstack) ?? fnArgs[1].EvalObjectArray(scope, inst, cstack);




    }



}
