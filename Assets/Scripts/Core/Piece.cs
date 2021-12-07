namespace Chess {
	public static class Piece {

		public const int None = 0;
		public const int King = 1;
		public const int Pawn = 2;
		public const int Knight = 3;
		public const int Bishop = 5;
		public const int Rook = 6;
		public const int Queen = 7;
		public const int Amazon = 8;
		public const int Champion = 9;
		public const int Wizard = 10;
		public const int Chancellor = 11;
		public const int Archbishop = 12;
		public const int Falcon = 13;
		public const int Hunter = 14;
		public const int Silver_General = 15;
		public const int Gold_General = 16;
		public const int Lance = 17;
		public const int Dragon_Horse = 18;
		public const int Dragon_King = 19;
		public const int Ultima_Pawn = 20;
		public const int Withdrawer = 21;
		public const int Long_Leaper = 22;
		public const int Coordinator = 23;
		public const int Immobilizer = 24;
		public const int Elephant = 25;
		public const int Cannon = 26;
		public const int Mao = 27;
		public const int Guard = 28;

		public const int White = 29;
		public const int Black = 58;

		const int typeMask = 0b00111;
		const int blackMask = 0b10000;
		const int whiteMask = 0b01000;
		const int colourMask = whiteMask | blackMask;

		public static bool IsColour (int piece, int colour) {
			return (piece & colourMask) == colour;
		}

		public static int Colour (int piece) {
			return piece & colourMask;
		}

		public static int PieceType (int piece) {
			return piece & typeMask;
		}

		public static bool IsRookOrQueen (int piece) {
			return (piece & 0b110) == 0b110;
		}

		public static bool IsBishopOrQueen (int piece) {
			return (piece & 0b101) == 0b101;
		}

		public static bool IsSlidingPiece (int piece) {
			return (piece & 0b100) != 0;
		}
	}
}