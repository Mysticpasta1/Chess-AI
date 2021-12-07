namespace Chess {

	public static class BoardRepresentation {
		public const string fileNames = "abcdefghijklmnopqrstuvwx";
		public const string rankNames = "123456789101112131415161718192021222324";

		public const int a1 = 0;
		public const int b1 = 1;
		public const int c1 = 2;
		public const int d1 = 3;
		public const int e1 = 4;
		public const int f1 = 5;
		public const int g1 = 6;
		public const int h1 = 7;
		public const int i1 = 8;
		public const int j1 = 9;
		public const int k1 = 10;
		public const int l1 = 11;
		public const int m1 = 12;
		public const int n1 = 13;
		public const int o1 = 14;
		public const int p1 = 15;
		public const int q1 = 16;
		public const int r1 = 17;
		public const int s1 = 18;
		public const int t1 = 19;
		public const int u1 = 20;
		public const int v1 = 21;
		public const int w1 = 22;
		public const int x1 = 23;

		public const int a24 = 551;
		public const int b24 = 552;
		public const int c24 = 553;
		public const int d24 = 554;
		public const int e24 = 555;
		public const int f24 = 556;
		public const int g24 = 557;
		public const int h24 = 558;
		public const int i24 = 559;
		public const int j24 = 560;
		public const int k24 = 561;
		public const int l24 = 562;
		public const int m24 = 563;
		public const int n24 = 564;
		public const int o24 = 565;
		public const int p24 = 567;
		public const int q24 = 568;
		public const int r24 = 569;
		public const int s24 = 570;
		public const int t24 = 571;
		public const int u24 = 572;
		public const int v24 = 573;
		public const int w24 = 574;
		public const int x24 = 575;

		// Rank (0 to 7) of square 
		public static int RankIndex (int squareIndex) {
			return squareIndex >> 3;
		}

		// File (0 to 7) of square 
		public static int FileIndex (int squareIndex) {
			return squareIndex & 0b000111;
		}

		public static int IndexFromCoord (int fileIndex, int rankIndex) {
			return rankIndex * 8 + fileIndex;
		}

		public static int IndexFromCoord (Coord coord) {
			return IndexFromCoord (coord.fileIndex, coord.rankIndex);
		}

		public static Coord CoordFromIndex (int squareIndex) {
			return new Coord (FileIndex (squareIndex), RankIndex (squareIndex));
		}

		public static bool LightSquare (int fileIndex, int rankIndex) {
			return (fileIndex + rankIndex) % 2 != 0;
		}

		public static string SquareNameFromCoordinate (int fileIndex, int rankIndex) {
			return fileNames[fileIndex] + "" + (rankIndex + 1);
		}

		public static string SquareNameFromIndex (int squareIndex) {
			return SquareNameFromCoordinate (CoordFromIndex (squareIndex));
		}

		public static string SquareNameFromCoordinate (Coord coord) {
			return SquareNameFromCoordinate (coord.fileIndex, coord.rankIndex);
		}
	}
}