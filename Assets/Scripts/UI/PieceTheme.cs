using UnityEngine;

namespace Chess.Game {
	[CreateAssetMenu (menuName = "Theme/Pieces")]
	public class PieceTheme : ScriptableObject {

		public PieceSprites whitePieces;
		public PieceSprites blackPieces;

		public Sprite GetPieceSprite (int piece) {
			PieceSprites pieceSprites = Piece.IsColour (piece, Piece.White) ? whitePieces : blackPieces;

			switch (Piece.PieceType (piece)) {
				case Piece.Pawn:
					return pieceSprites.pawn;
				case Piece.Rook:
					return pieceSprites.rook;
				case Piece.Knight:
					return pieceSprites.knight;
				case Piece.Bishop:
					return pieceSprites.bishop;
				case Piece.Queen:
					return pieceSprites.queen;
				case Piece.King:
					return pieceSprites.king;
				case Piece.Amazon:
					return pieceSprites.amazon;
				case Piece.Champion:
					return pieceSprites.champion;
				case Piece.Wizard:
					return pieceSprites.wizard;
				case Piece.Chancellor:
					return pieceSprites.chancellor;
				case Piece.Archbishop:
					return pieceSprites.archbishop;
				case Piece.Falcon:
					return pieceSprites.falcon;
				case Piece.Hunter:
					return pieceSprites.hunter;
				case Piece.Silver_General:
					return pieceSprites.silver_general;
				case Piece.Gold_General:
					return pieceSprites.gold_general;
				case Piece.Lance:
					return pieceSprites.lance;
				case Piece.Dragon_Horse:
					return pieceSprites.dragon_horse;
				case Piece.Dragon_King:
					return pieceSprites.dragon_king;
				case Piece.Ultima_Pawn:
					return pieceSprites.ultima_pawn;
				case Piece.Withdrawer:
					return pieceSprites.withdrawer;
				case Piece.Long_Leaper:
					return pieceSprites.long_leaper;
				case Piece.Coordinator:
					return pieceSprites.coordinator;
				case Piece.Immobilizer:
					return pieceSprites.immobilizer;
				case Piece.Elephant:
					return pieceSprites.elephant;
				case Piece.Mao:
					return pieceSprites.mao;
				case Piece.Cannon:
					return pieceSprites.cannon;
				case Piece.Guard:
					return pieceSprites.guard;
				default:
					if (piece != 0) {
						Debug.Log (piece);
					}
					return null;
			}
		}

		[System.Serializable]
		public class PieceSprites {
			public Sprite pawn, rook, knight, bishop, queen, king, amazon, champion, wizard, chancellor, archbishop, falcon, hunter, silver_general, gold_general, lance, dragon_horse, dragon_king, ultima_pawn, withdrawer, long_leaper, coordinator, immobilizer, elephant, cannon, mao, guard;

			public Sprite this [int i] {
				get {
					return new Sprite[] { pawn, rook, knight, bishop, queen, king, amazon, champion, wizard, chancellor, archbishop, falcon, hunter, silver_general, gold_general, lance, dragon_horse, dragon_king, ultima_pawn, withdrawer, long_leaper, coordinator, immobilizer, elephant, cannon, mao, guard }[i];
				}
			}
		}
	}
}