namespace Chess {
	using System.Collections.Generic;

	public class Board {

		public const int WhiteIndex = 0;
		public const int BlackIndex = 1;

		// Stores piece code for each square on the board.
		// Piece code is defined as piecetype | colour code
		public int[] Square;

		public bool WhiteToMove;
		public int ColourToMove;
		public int OpponentColour;
		public int ColourToMoveIndex;

		// Bits 0-3 store white and black kingside/queenside castling legality
		// Bits 4-7 store file of ep square (starting at 1, so 0 = no ep square)
		// Bits 8-13 captured piece
		// Bits 14-... fifty mover counter
		Stack<uint> gameStateHistory;
		public uint currentGameState;

		public int plyCount; // Total plies played in game
		public int fiftyMoveCounter; // Num ply since last pawn move or capture

		public ulong ZobristKey;
		/// List of zobrist keys 
		public Stack<ulong> RepetitionPositionHistory;

		public int[] KingSquare; // index of square of white and black king

		public PieceList[] rooks;
		public PieceList[] bishops;
		public PieceList[] queens;
		public PieceList[] knights;
		public PieceList[] pawns;
		public PieceList[] dragon_kings;
		public PieceList[] amazons;
		public PieceList[] champions;
		public PieceList[] wizards;
		public PieceList[] chancellors;
		public PieceList[] archbishops;
		public PieceList[] falcons;
		public PieceList[] hunters;
		public PieceList[] silver_generals;
		public PieceList[] gold_generals;
		public PieceList[] lances;
		public PieceList[] dragon_horses;
		public PieceList[] ultima_pawns;
		public PieceList[] withdrawers;
		public PieceList[] long_leapers;
		public PieceList[] coordinators;
		public PieceList[] immobilizors;
		public PieceList[] elephants;
		public PieceList[] cannons;
		public PieceList[] maos;
		public PieceList[] guards;

		PieceList[] allPieceLists;

		const uint whiteCastleKingsideMask = 0b1111111111111110;
		const uint whiteCastleQueensideMask = 0b1111111111111101;
		const uint blackCastleKingsideMask = 0b1111111111111011;
		const uint blackCastleQueensideMask = 0b1111111111110111;

		const uint whiteCastleMask = whiteCastleKingsideMask & whiteCastleQueensideMask;
		const uint blackCastleMask = blackCastleKingsideMask & blackCastleQueensideMask;

		PieceList GetPieceList (int pieceType, int colourIndex) {
			return allPieceLists[colourIndex * 8 + pieceType];
		}

		// Make a move on the board
		// The inSearch parameter controls whether this move should be recorded in the game history (for detecting three-fold repetition)
		public void MakeMove (Move move, bool inSearch = false) {
			uint oldEnPassantFile = (currentGameState >> 4) & 15;
			uint originalCastleState = currentGameState & 15;
			uint newCastleState = originalCastleState;
			currentGameState = 0;

			int opponentColourIndex = 1 - ColourToMoveIndex;
			int moveFrom = move.StartSquare;
			int moveTo = move.TargetSquare;

			int capturedPieceType = Piece.PieceType (Square[moveTo]);
			int movePiece = Square[moveFrom];
			int movePieceType = Piece.PieceType (movePiece);

			int moveFlag = move.MoveFlag;
			bool isPromotion = move.IsPromotion;
			bool isEnPassant = moveFlag == Move.Flag.EnPassantCapture;

			// Handle captures
			currentGameState |= (ushort) (capturedPieceType << 8);
			if (capturedPieceType != 0 && !isEnPassant) {
				ZobristKey ^= Zobrist.piecesArray[capturedPieceType, opponentColourIndex, moveTo];
				GetPieceList (capturedPieceType, opponentColourIndex).RemovePieceAtSquare (moveTo);
			}

			// Move pieces in piece lists
			if (movePieceType == Piece.King) {
				KingSquare[ColourToMoveIndex] = moveTo;
				newCastleState &= (WhiteToMove) ? whiteCastleMask : blackCastleMask;
			} else {
				GetPieceList (movePieceType, ColourToMoveIndex).MovePiece (moveFrom, moveTo);
			}

			int pieceOnTargetSquare = movePiece;

			// Handle promotion
			if (isPromotion) {
				int promoteType = 0;
				switch (moveFlag) {
					case Move.Flag.PromoteToQueen:
						promoteType = Piece.Queen;
						queens[ColourToMoveIndex].AddPieceAtSquare (moveTo);
						break;
					case Move.Flag.PromoteToRook:
						promoteType = Piece.Rook;
						rooks[ColourToMoveIndex].AddPieceAtSquare (moveTo);
						break;
					case Move.Flag.PromoteToBishop:
						promoteType = Piece.Bishop;
						bishops[ColourToMoveIndex].AddPieceAtSquare (moveTo);
						break;
					case Move.Flag.PromoteToKnight:
						promoteType = Piece.Knight;
						knights[ColourToMoveIndex].AddPieceAtSquare (moveTo);
						break;

				}
				pieceOnTargetSquare = promoteType | ColourToMove;
				pawns[ColourToMoveIndex].RemovePieceAtSquare (moveTo);
			} else {
				// Handle other special moves (en-passant, and castling)
				switch (moveFlag) {
					case Move.Flag.EnPassantCapture:
						int epPawnSquare = moveTo + ((ColourToMove == Piece.White) ? -8 : 8);
						currentGameState |= (ushort) (Square[epPawnSquare] << 8); // add pawn as capture type
						Square[epPawnSquare] = 0; // clear ep capture square
						pawns[opponentColourIndex].RemovePieceAtSquare (epPawnSquare);
						ZobristKey ^= Zobrist.piecesArray[Piece.Pawn, opponentColourIndex, epPawnSquare];
						break;
				}
			}

			// Update the board representation:
			Square[moveTo] = pieceOnTargetSquare;
			Square[moveFrom] = 0;

			// Pawn has moved two forwards, mark file with en-passant flag
			if (moveFlag == Move.Flag.PawnTwoForward) {
				int file = BoardRepresentation.FileIndex (moveFrom) + 1;
				currentGameState |= (ushort) (file << 4);
				ZobristKey ^= Zobrist.enPassantFile[file];
			}

			// Update zobrist key with new piece position and side to move
			ZobristKey ^= Zobrist.sideToMove;
			ZobristKey ^= Zobrist.piecesArray[movePieceType, ColourToMoveIndex, moveFrom];
			ZobristKey ^= Zobrist.piecesArray[Piece.PieceType (pieceOnTargetSquare), ColourToMoveIndex, moveTo];

			if (oldEnPassantFile != 0)
				ZobristKey ^= Zobrist.enPassantFile[oldEnPassantFile];

			if (newCastleState != originalCastleState) {
				ZobristKey ^= Zobrist.castlingRights[originalCastleState]; // remove old castling rights state
				ZobristKey ^= Zobrist.castlingRights[newCastleState]; // add new castling rights state
			}
			currentGameState |= newCastleState;
			currentGameState |= (uint) fiftyMoveCounter << 14;
			gameStateHistory.Push (currentGameState);

			// Change side to move
			WhiteToMove = !WhiteToMove;
			ColourToMove = (WhiteToMove) ? Piece.White : Piece.Black;
			OpponentColour = (WhiteToMove) ? Piece.Black : Piece.White;
			ColourToMoveIndex = 1 - ColourToMoveIndex;
			plyCount++;
			fiftyMoveCounter++;

			if (!inSearch) {
				if (movePieceType == Piece.Pawn || capturedPieceType != Piece.None) {
					RepetitionPositionHistory.Clear ();
					fiftyMoveCounter = 0;
				} else {
					RepetitionPositionHistory.Push (ZobristKey);
				}
			}

		}

		// Undo a move previously made on the board
		public void UnmakeMove (Move move, bool inSearch = false) {

			//int opponentColour = ColourToMove;
			int opponentColourIndex = ColourToMoveIndex;
			bool undoingWhiteMove = OpponentColour == Piece.White;
			ColourToMove = OpponentColour; // side who made the move we are undoing
			OpponentColour = (undoingWhiteMove) ? Piece.Black : Piece.White;
			ColourToMoveIndex = 1 - ColourToMoveIndex;
			WhiteToMove = !WhiteToMove;

			uint originalCastleState = currentGameState & 0b1111;

			int capturedPieceType = ((int) currentGameState >> 8) & 63;
			int capturedPiece = (capturedPieceType == 0) ? 0 : capturedPieceType | OpponentColour;

			int movedFrom = move.StartSquare;
			int movedTo = move.TargetSquare;
			int moveFlags = move.MoveFlag;
			bool isEnPassant = moveFlags == Move.Flag.EnPassantCapture;
			bool isPromotion = move.IsPromotion;

			int toSquarePieceType = Piece.PieceType (Square[movedTo]);
			int movedPieceType = (isPromotion) ? Piece.Pawn : toSquarePieceType;

			// Update zobrist key with new piece position and side to move
			ZobristKey ^= Zobrist.sideToMove;
			ZobristKey ^= Zobrist.piecesArray[movedPieceType, ColourToMoveIndex, movedFrom]; // add piece back to square it moved from
			ZobristKey ^= Zobrist.piecesArray[toSquarePieceType, ColourToMoveIndex, movedTo]; // remove piece from square it moved to

			uint oldEnPassantFile = (currentGameState >> 4) & 15;
			if (oldEnPassantFile != 0)
				ZobristKey ^= Zobrist.enPassantFile[oldEnPassantFile];

			// ignore ep captures, handled later
			if (capturedPieceType != 0 && !isEnPassant) {
				ZobristKey ^= Zobrist.piecesArray[capturedPieceType, opponentColourIndex, movedTo];
				GetPieceList (capturedPieceType, opponentColourIndex).AddPieceAtSquare (movedTo);
			}

			// Update king index
			if (movedPieceType == Piece.King) {
				KingSquare[ColourToMoveIndex] = movedFrom;
			} else if (!isPromotion) {
				GetPieceList (movedPieceType, ColourToMoveIndex).MovePiece (movedTo, movedFrom);
			}

			// put back moved piece
			Square[movedFrom] = movedPieceType | ColourToMove; // note that if move was a pawn promotion, this will put the promoted piece back instead of the pawn. Handled in special move switch
			Square[movedTo] = capturedPiece; // will be 0 if no piece was captured

			if (isPromotion) {
				pawns[ColourToMoveIndex].AddPieceAtSquare (movedFrom);
				switch (moveFlags) {
					case Move.Flag.PromoteToQueen:
						queens[ColourToMoveIndex].RemovePieceAtSquare (movedTo);
						break;
					case Move.Flag.PromoteToKnight:
						knights[ColourToMoveIndex].RemovePieceAtSquare (movedTo);
						break;
					case Move.Flag.PromoteToRook:
						rooks[ColourToMoveIndex].RemovePieceAtSquare (movedTo);
						break;
					case Move.Flag.PromoteToBishop:
						bishops[ColourToMoveIndex].RemovePieceAtSquare (movedTo);
						break;
				}
			} else if (isEnPassant) { // ep cature: put captured pawn back on right square
				int epIndex = movedTo + ((ColourToMove == Piece.White) ? -8 : 8);
				Square[movedTo] = 0;
				Square[epIndex] = (int) capturedPiece;
				pawns[opponentColourIndex].AddPieceAtSquare (epIndex);
				ZobristKey ^= Zobrist.piecesArray[Piece.Pawn, opponentColourIndex, epIndex];
			} else if (moveFlags == Move.Flag.Castling) { // castles: move rook back to starting square

				bool kingside = movedTo == 6 || movedTo == 62;
				int castlingRookFromIndex = (kingside) ? movedTo + 1 : movedTo - 2;
				int castlingRookToIndex = (kingside) ? movedTo - 1 : movedTo + 1;

				Square[castlingRookToIndex] = 0;
				Square[castlingRookFromIndex] = Piece.Rook | ColourToMove;

				rooks[ColourToMoveIndex].MovePiece (castlingRookToIndex, castlingRookFromIndex);
				ZobristKey ^= Zobrist.piecesArray[Piece.Rook, ColourToMoveIndex, castlingRookFromIndex];
				ZobristKey ^= Zobrist.piecesArray[Piece.Rook, ColourToMoveIndex, castlingRookToIndex];

			}

			gameStateHistory.Pop (); // removes current state from history
			currentGameState = gameStateHistory.Peek (); // sets current state to previous state in history

			fiftyMoveCounter = (int) (currentGameState & 4294950912) >> 14;
			int newEnPassantFile = (int) (currentGameState >> 4) & 15;
			if (newEnPassantFile != 0)
				ZobristKey ^= Zobrist.enPassantFile[newEnPassantFile];

			uint newCastleState = currentGameState & 0b1111;
			if (newCastleState != originalCastleState) {
				ZobristKey ^= Zobrist.castlingRights[originalCastleState]; // remove old castling rights state
				ZobristKey ^= Zobrist.castlingRights[newCastleState]; // add new castling rights state
			}

			plyCount--;

			if (!inSearch && RepetitionPositionHistory.Count > 0) {
				RepetitionPositionHistory.Pop ();
			}

		}

		// Load the starting position
		public void LoadStartPosition () {
			LoadPosition(FenUtility.startFen);
		}

		public int isColor()
		{
			int colorIndex = WhiteIndex;
			for (int color = 0; color < 576; color++)
			{
				if (color < 300)
				{
					colorIndex = WhiteIndex;
				}
				else
				{
					colorIndex = BlackIndex;
				}
			}
			return colorIndex;
        }

		// Load custom position from fen string
		public void LoadPosition (string fen) {
			Initialize ();
			var loadedPosition = FenUtility.PositionFromFen(fen);
			int[] pieces = new int[]
			{
				Piece.Rook, Piece.Gold_General, Piece.Ultima_Pawn, Piece.Ultima_Pawn, Piece.Ultima_Pawn, Piece.Ultima_Pawn,
				Piece.Archbishop, Piece.Chancellor, Piece.Knight, Piece.Knight, Piece.Dragon_Horse, Piece.Dragon_King,
				Piece.King, Piece.Queen + Piece.Knight, Piece.Knight, Piece.Chancellor, Piece.Archbishop, Piece.Ultima_Pawn,
				Piece.Ultima_Pawn, Piece.Ultima_Pawn, Piece.Ultima_Pawn, Piece.Silver_General, Piece.Rook, Piece.Lance,
				Piece.Hunter, Piece.Elephant, Piece.Long_Leaper, Piece.Mao, Piece.Guard, Piece.Bishop, Piece.Wizard,
				Piece.Amazon, Piece.Amazon, Piece.Cannon, Piece.Champion, Piece.Champion, Piece.Cannon, Piece.Amazon,
				Piece.Amazon, Piece.Wizard, Piece.Bishop, Piece.Guard, Piece.Mao, Piece.Long_Leaper, Piece.Elephant,
				Piece.Falcon, Piece.Lance, Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn,
				Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn,
				Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn,
				Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn,Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,  Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,  Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,  Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,  Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,  Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,  Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,  Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,  Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None, Piece.None,
				Piece.None, Piece.None, Piece.Rook, Piece.Silver_General, Piece.Ultima_Pawn, Piece.Ultima_Pawn, Piece.Ultima_Pawn, Piece.Ultima_Pawn,
				Piece.Archbishop, Piece.Chancellor, Piece.Knight, Piece.Knight, Piece.Dragon_Horse, Piece.Dragon_King,
				Piece.King, Piece.Queen + Piece.Knight, Piece.Knight, Piece.Chancellor, Piece.Archbishop, Piece.Ultima_Pawn,
				Piece.Ultima_Pawn, Piece.Ultima_Pawn, Piece.Ultima_Pawn, Piece.Gold_General, Piece.Rook, Piece.Lance,
				Piece.Falcon, Piece.Elephant, Piece.Long_Leaper, Piece.Mao, Piece.Guard, Piece.Bishop, Piece.Wizard,
				Piece.Amazon, Piece.Amazon, Piece.Cannon, Piece.Champion, Piece.Champion, Piece.Cannon, Piece.Amazon,
				Piece.Amazon, Piece.Wizard, Piece.Bishop, Piece.Guard, Piece.Mao, Piece.Long_Leaper, Piece.Elephant,
				Piece.Hunter, Piece.Lance, Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn,
				Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn,
				Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn,
				Piece.Pawn, Piece.Pawn, Piece.Pawn, Piece.Pawn
			};
			// Load pieces into board array and piece lists
			for (int squareIndex = 0; squareIndex <  576; squareIndex++) {

				int piece = loadedPosition.squares[squareIndex];
				int pieceColourIndex = (Piece.IsColour(piece, Piece.White)) ? WhiteIndex : BlackIndex;

				if (piece != Piece.None)
				{

					if (Piece.IsSlidingPiece(piece))
					{
						if (piece == Piece.Queen)
						{
							queens[pieceColourIndex].AddPieceAtSquare(squareIndex);
						}
						else if (piece == Piece.Rook)
						{
							rooks[pieceColourIndex].AddPieceAtSquare(squareIndex);
						}
						else if (piece == Piece.Bishop)
						{
							bishops[pieceColourIndex].AddPieceAtSquare(squareIndex);
						}
						else if (piece == Piece.Amazon)
						{
							amazons[pieceColourIndex].AddPieceAtSquare(squareIndex);
						}
						else if (piece == Piece.Falcon)
						{
							falcons[pieceColourIndex].AddPieceAtSquare(squareIndex);
						}
						else if (piece == Piece.Hunter)
						{
							hunters[pieceColourIndex].AddPieceAtSquare(squareIndex);
						}
						else if (piece == Piece.Chancellor)
						{
							chancellors[pieceColourIndex].AddPieceAtSquare(squareIndex);
						}
						else if (piece == Piece.Archbishop)
						{
							archbishops[pieceColourIndex].AddPieceAtSquare(squareIndex);
						}
						else if (piece == Piece.Dragon_Horse)
						{
							dragon_horses[pieceColourIndex].AddPieceAtSquare(squareIndex);
						}
						else if (piece == Piece.Dragon_King)
						{
							dragon_kings[pieceColourIndex].AddPieceAtSquare(squareIndex);
						}
						else if (piece == Piece.Cannon)
						{
							cannons[pieceColourIndex].AddPieceAtSquare(squareIndex);
						}
						else if (piece == Piece.Immobilizer)
						{
							immobilizors[pieceColourIndex].AddPieceAtSquare(squareIndex);
						}
						else if (piece == Piece.Long_Leaper)
						{
							long_leapers[pieceColourIndex].AddPieceAtSquare(squareIndex);
						}
						else if (piece == Piece.Coordinator)
						{
							coordinators[pieceColourIndex].AddPieceAtSquare(squareIndex);
						}
						else if (piece == Piece.Withdrawer)
						{
							withdrawers[pieceColourIndex].AddPieceAtSquare(squareIndex);
						}
						else if (piece == Piece.Lance)
						{
							lances[pieceColourIndex].AddPieceAtSquare(squareIndex);
						}
					}
					else if (piece == Piece.Knight)
					{
						knights[pieceColourIndex].AddPieceAtSquare(squareIndex);
					}
					else if (piece == Piece.Pawn)
					{
						pawns[pieceColourIndex].AddPieceAtSquare(squareIndex);
					}
					else if (piece == Piece.King)
					{
						KingSquare[pieceColourIndex] = squareIndex;
					}
					else if (piece == Piece.Ultima_Pawn)
					{
						ultima_pawns[pieceColourIndex].AddPieceAtSquare(squareIndex);
					}
					else if (piece == Piece.Elephant)
					{
						elephants[pieceColourIndex].AddPieceAtSquare(squareIndex);
					}
					else if (piece == Piece.Mao)
					{
						maos[pieceColourIndex].AddPieceAtSquare(squareIndex);
					}
					else if (piece == Piece.Guard)
					{
						guards[pieceColourIndex].AddPieceAtSquare(squareIndex);
					}
					else if (piece == Piece.Silver_General)
					{
						silver_generals[pieceColourIndex].AddPieceAtSquare(squareIndex);
					}
					else if (piece == Piece.Gold_General)
					{
						gold_generals[pieceColourIndex].AddPieceAtSquare(squareIndex);
					}
					else if (piece == Piece.Wizard)
					{
						wizards[pieceColourIndex].AddPieceAtSquare(squareIndex);
					}
					else if (piece == Piece.Champion)
					{
						champions[pieceColourIndex].AddPieceAtSquare(squareIndex);
					}
				}
			}

			// Side to move
			WhiteToMove = loadedPosition.whiteToMove;
			ColourToMove = (WhiteToMove) ? Piece.White : Piece.Black;
			OpponentColour = (WhiteToMove) ? Piece.Black : Piece.White;
			ColourToMoveIndex = (WhiteToMove) ? 0 : 1;

			// Create gamestate
			int whiteCastle = ((loadedPosition.whiteCastleKingside) ? 1 << 0 : 0) | ((loadedPosition.whiteCastleQueenside) ? 1 << 1 : 0);
			int blackCastle = ((loadedPosition.blackCastleKingside) ? 1 << 2 : 0) | ((loadedPosition.blackCastleQueenside) ? 1 << 3 : 0);
			int epState = loadedPosition.epFile << 4;
			ushort initialGameState = (ushort) (whiteCastle | blackCastle | epState);
			gameStateHistory.Push (initialGameState);
			currentGameState = initialGameState;
			plyCount = loadedPosition.plyCount;

			// Initialize zobrist key
			ZobristKey = Zobrist.CalculateZobristKey (this);
		}

		void Initialize () {
			Square = new int[576];
			KingSquare = new int[2];

			gameStateHistory = new Stack<uint> ();
			ZobristKey = 0;
			RepetitionPositionHistory = new Stack<ulong> ();
			plyCount = 0;
			fiftyMoveCounter = 0;

			knights = new PieceList[] { new PieceList (26), new PieceList (26) };
			pawns = new PieceList[] { new PieceList (24), new PieceList (24) };
			rooks = new PieceList[] { new PieceList (26), new PieceList (26) };
			bishops = new PieceList[] { new PieceList (26), new PieceList (26)};
			queens = new PieceList[] { new PieceList (25), new PieceList (25) };
			amazons = new PieceList[] { new PieceList(4), new PieceList(4) };
			gold_generals = new PieceList[] { new PieceList(1), new PieceList(1) };
			silver_generals = new PieceList[] { new PieceList(1), new PieceList(1) };
			ultima_pawns = new PieceList[] { new PieceList(8), new PieceList(8) };
			cannons = new PieceList[] { new PieceList(2), new PieceList(2) };
		    archbishops = new PieceList[] { new PieceList(2), new PieceList(2) };
			lances = new PieceList[] { new PieceList(2), new PieceList(2) };
			maos = new PieceList[] { new PieceList(2), new PieceList(2) };
			elephants = new PieceList[] { new PieceList(2), new PieceList(2) };
			guards = new PieceList[] { new PieceList(2), new PieceList(2) };
			falcons = new PieceList[] { new PieceList(1), new PieceList(1) };
			hunters = new PieceList[] { new PieceList(1), new PieceList(1) };
			dragon_kings = new PieceList[] { new PieceList(1), new PieceList(1) };
			dragon_horses = new PieceList[] { new PieceList(1), new PieceList(1) };
			champions = new PieceList[] { new PieceList(2), new PieceList(2) };
			chancellors = new PieceList[] { new PieceList(2), new PieceList(2) };
			wizards = new PieceList[] { new PieceList(2), new PieceList(2) };
			long_leapers = new PieceList[] { new PieceList(2), new PieceList(2) };
			coordinators = new PieceList[] { new PieceList(2), new PieceList(2) };
			withdrawers = new PieceList[] { new PieceList(2), new PieceList(2) };
			immobilizors = new PieceList[] { new PieceList(2), new PieceList(2) };
			PieceList emptyList = new PieceList (0);
			allPieceLists = new PieceList[] {
				emptyList,
				emptyList,
				pawns[WhiteIndex],
				knights[WhiteIndex],
				champions[WhiteIndex],
				wizards[WhiteIndex],
				silver_generals[WhiteIndex],
				gold_generals[WhiteIndex],
				ultima_pawns[WhiteIndex],
				elephants[WhiteIndex],
				maos[WhiteIndex],
				guards[WhiteIndex],
				emptyList,
				bishops[WhiteIndex],
				rooks[WhiteIndex],
				queens[WhiteIndex],
				amazons[WhiteIndex],
				falcons[WhiteIndex],
				hunters[WhiteIndex],
				chancellors[WhiteIndex],
				archbishops[WhiteIndex],
				withdrawers[WhiteIndex],
				long_leapers[WhiteIndex],
				coordinators[WhiteIndex],
				immobilizors[WhiteIndex],
				cannons[WhiteIndex],
				lances[WhiteIndex],
				dragon_horses[WhiteIndex],
				dragon_kings[WhiteIndex],
				emptyList,
				emptyList,
				pawns[BlackIndex],
				knights[BlackIndex],
				champions[BlackIndex],
				wizards[BlackIndex],
				silver_generals[BlackIndex],
				gold_generals[BlackIndex],
				ultima_pawns[BlackIndex],
				elephants[BlackIndex],
				maos[BlackIndex],
				guards[BlackIndex],
				emptyList,
				bishops[BlackIndex],
				rooks[BlackIndex],
				queens[BlackIndex],
				amazons[BlackIndex],
				falcons[BlackIndex],
				hunters[BlackIndex],
				chancellors[BlackIndex],
				archbishops[BlackIndex],
				withdrawers[BlackIndex],
				long_leapers[BlackIndex],
				coordinators[BlackIndex],
				immobilizors[BlackIndex],
				cannons[BlackIndex],
				dragon_horses[BlackIndex],
				dragon_kings[BlackIndex],
				lances[BlackIndex]
			};
		}
	}
}