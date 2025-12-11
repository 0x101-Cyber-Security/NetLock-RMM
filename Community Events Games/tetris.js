// Tetris Game Implementation
window.tetris = (function() {
    let canvas, ctx;
    let gameBoard = [];
    let currentPiece = null;
    let currentX = 0;
    let currentY = 0;
    let score = 0;
    let level = 1;
    let lines = 0;
    let gameLoop = null;
    let dropCounter = 0;
    let dropInterval = 1000;
    let lastTime = 0;
    let isPaused = false;
    let isGameOver = false;

    const COLS = 10;
    const ROWS = 20;
    const BLOCK_SIZE = 30;

    // Tetromino shapes
    const SHAPES = {
        I: [[1, 1, 1, 1]],
        O: [[1, 1], [1, 1]],
        T: [[0, 1, 0], [1, 1, 1]],
        S: [[0, 1, 1], [1, 1, 0]],
        Z: [[1, 1, 0], [0, 1, 1]],
        J: [[1, 0, 0], [1, 1, 1]],
        L: [[0, 0, 1], [1, 1, 1]]
    };

    const COLORS = {
        I: '#00f0f0',
        O: '#f0f000',
        T: '#a000f0',
        S: '#00f000',
        Z: '#f00000',
        J: '#0000f0',
        L: '#f0a000'
    };

    function init() {
        canvas = document.getElementById('tetrisCanvas');
        if (!canvas) {
            console.error('Canvas element not found');
            return false;
        }
        ctx = canvas.getContext('2d');
        
        // Initialize game board
        gameBoard = Array(ROWS).fill().map(() => Array(COLS).fill(0));
        
        // Setup keyboard controls
        document.addEventListener('keydown', handleKeyPress);
        
        return true;
    }

    function handleKeyPress(e) {
        if (!currentPiece || isGameOver) return;
        
        if (e.key === 'p' || e.key === 'P') {
            isPaused = !isPaused;
            return;
        }
        
        if (isPaused) return;

        switch(e.key) {
            case 'ArrowLeft':
                movePiece(-1, 0);
                e.preventDefault();
                break;
            case 'ArrowRight':
                movePiece(1, 0);
                e.preventDefault();
                break;
            case 'ArrowDown':
                movePiece(0, 1);
                dropCounter = 0;
                e.preventDefault();
                break;
            case 'ArrowUp':
                rotatePiece();
                e.preventDefault();
                break;
            case ' ':
                hardDrop();
                e.preventDefault();
                break;
        }
    }

    function startGame() {
        if (!init()) {
            setTimeout(startGame, 100);
            return;
        }
        
        // Reset game state
        gameBoard = Array(ROWS).fill().map(() => Array(COLS).fill(0));
        score = 0;
        level = 1;
        lines = 0;
        isPaused = false;
        isGameOver = false;
        dropInterval = 1000;
        
        updateScore();
        spawnPiece();
        
        if (gameLoop) {
            cancelAnimationFrame(gameLoop);
        }
        
        lastTime = 0;
        gameLoop = requestAnimationFrame(update);
    }

    function update(time = 0) {
        if (isGameOver) {
            drawGameOver();
            return;
        }

        const deltaTime = time - lastTime;
        lastTime = time;

        if (!isPaused) {
            dropCounter += deltaTime;
            
            if (dropCounter > dropInterval) {
                if (!movePiece(0, 1)) {
                    lockPiece();
                    clearLines();
                    spawnPiece();
                    
                    if (checkCollision(currentPiece.shape, currentX, currentY)) {
                        isGameOver = true;
                    }
                }
                dropCounter = 0;
            }
        }

        draw();
        gameLoop = requestAnimationFrame(update);
    }

    function spawnPiece() {
        const shapes = Object.keys(SHAPES);
        const randomShape = shapes[Math.floor(Math.random() * shapes.length)];
        
        currentPiece = {
            shape: SHAPES[randomShape],
            color: COLORS[randomShape],
            type: randomShape
        };
        
        currentX = Math.floor(COLS / 2) - Math.floor(currentPiece.shape[0].length / 2);
        currentY = 0;
    }

    function movePiece(dx, dy) {
        const newX = currentX + dx;
        const newY = currentY + dy;
        
        if (!checkCollision(currentPiece.shape, newX, newY)) {
            currentX = newX;
            currentY = newY;
            return true;
        }
        return false;
    }

    function rotatePiece() {
        const rotated = currentPiece.shape[0].map((_, i) =>
            currentPiece.shape.map(row => row[i]).reverse()
        );
        
        if (!checkCollision(rotated, currentX, currentY)) {
            currentPiece.shape = rotated;
        } else {
            // Wall kick - try moving left or right
            if (!checkCollision(rotated, currentX - 1, currentY)) {
                currentPiece.shape = rotated;
                currentX--;
            } else if (!checkCollision(rotated, currentX + 1, currentY)) {
                currentPiece.shape = rotated;
                currentX++;
            }
        }
    }

    function hardDrop() {
        while (movePiece(0, 1)) {
            score += 2;
        }
        lockPiece();
        clearLines();
        spawnPiece();
        updateScore();
    }

    function checkCollision(shape, x, y) {
        for (let row = 0; row < shape.length; row++) {
            for (let col = 0; col < shape[row].length; col++) {
                if (shape[row][col]) {
                    const newX = x + col;
                    const newY = y + row;
                    
                    if (newX < 0 || newX >= COLS || newY >= ROWS) {
                        return true;
                    }
                    
                    if (newY >= 0 && gameBoard[newY][newX]) {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    function lockPiece() {
        for (let row = 0; row < currentPiece.shape.length; row++) {
            for (let col = 0; col < currentPiece.shape[row].length; col++) {
                if (currentPiece.shape[row][col]) {
                    const y = currentY + row;
                    const x = currentX + col;
                    if (y >= 0) {
                        gameBoard[y][x] = currentPiece.color;
                    }
                }
            }
        }
    }

    function clearLines() {
        let linesCleared = 0;
        
        for (let row = ROWS - 1; row >= 0; row--) {
            if (gameBoard[row].every(cell => cell !== 0)) {
                gameBoard.splice(row, 1);
                gameBoard.unshift(Array(COLS).fill(0));
                linesCleared++;
                row++; // Check the same row again
            }
        }
        
        if (linesCleared > 0) {
            lines += linesCleared;
            
            // Scoring system
            const lineScores = [0, 100, 300, 500, 800];
            score += lineScores[linesCleared] * level;
            
            // Level up every 10 lines
            level = Math.floor(lines / 10) + 1;
            dropInterval = Math.max(100, 1000 - (level - 1) * 100);
            
            updateScore();
        }
    }

    function updateScore() {
        // Update desktop score
        document.getElementById('score').textContent = score;
        document.getElementById('level').textContent = level;
        document.getElementById('lines').textContent = lines;
        
        // Update mobile score if elements exist
        const scoreMobile = document.getElementById('score-mobile');
        const levelMobile = document.getElementById('level-mobile');
        const linesMobile = document.getElementById('lines-mobile');
        
        if (scoreMobile) scoreMobile.textContent = score;
        if (levelMobile) levelMobile.textContent = level;
        if (linesMobile) linesMobile.textContent = lines;
    }

    function draw() {
        // Clear canvas
        ctx.fillStyle = '#000';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        
        // Draw grid
        ctx.strokeStyle = '#333';
        ctx.lineWidth = 1;
        for (let row = 0; row < ROWS; row++) {
            for (let col = 0; col < COLS; col++) {
                ctx.strokeRect(col * BLOCK_SIZE, row * BLOCK_SIZE, BLOCK_SIZE, BLOCK_SIZE);
            }
        }
        
        // Draw locked pieces
        for (let row = 0; row < ROWS; row++) {
            for (let col = 0; col < COLS; col++) {
                if (gameBoard[row][col]) {
                    drawBlock(col, row, gameBoard[row][col]);
                }
            }
        }
        
        // Draw current piece
        if (currentPiece) {
            for (let row = 0; row < currentPiece.shape.length; row++) {
                for (let col = 0; col < currentPiece.shape[row].length; col++) {
                    if (currentPiece.shape[row][col]) {
                        drawBlock(currentX + col, currentY + row, currentPiece.color);
                    }
                }
            }
        }
        
        // Draw pause overlay
        if (isPaused) {
            ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            ctx.fillStyle = '#fff';
            ctx.font = '30px Arial';
            ctx.textAlign = 'center';
            ctx.fillText('PAUSED', canvas.width / 2, canvas.height / 2);
        }
    }

    function drawBlock(x, y, color) {
        ctx.fillStyle = color;
        ctx.fillRect(x * BLOCK_SIZE + 1, y * BLOCK_SIZE + 1, BLOCK_SIZE - 2, BLOCK_SIZE - 2);
        
        // Add some shading for 3D effect
        ctx.fillStyle = 'rgba(255, 255, 255, 0.3)';
        ctx.fillRect(x * BLOCK_SIZE + 1, y * BLOCK_SIZE + 1, BLOCK_SIZE - 2, 3);
        ctx.fillRect(x * BLOCK_SIZE + 1, y * BLOCK_SIZE + 1, 3, BLOCK_SIZE - 2);
    }

    function drawGameOver() {
        ctx.fillStyle = 'rgba(0, 0, 0, 0.8)';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        
        ctx.fillStyle = '#f00';
        ctx.font = 'bold 40px Arial';
        ctx.textAlign = 'center';
        ctx.fillText('GAME OVER', canvas.width / 2, canvas.height / 2 - 20);
        
        ctx.fillStyle = '#fff';
        ctx.font = '20px Arial';
        ctx.fillText('Press Start to play again', canvas.width / 2, canvas.height / 2 + 20);
    }

    function togglePause() {
        if (isGameOver || !currentPiece) {
            return;
        }
        isPaused = !isPaused;
    }

    function stopGame() {
        if (gameLoop) {
            cancelAnimationFrame(gameLoop);
            gameLoop = null;
        }
        
        // Clear the game board
        gameBoard = Array(ROWS).fill().map(() => Array(COLS).fill(0));
        currentPiece = null;
        isPaused = false;
        isGameOver = true;
        
        // Draw empty board with "Stopped" message
        draw();
        ctx.fillStyle = 'rgba(0, 0, 0, 0.8)';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        
        ctx.fillStyle = '#ff9800';
        ctx.font = 'bold 40px Arial';
        ctx.textAlign = 'center';
        ctx.fillText('STOPPED', canvas.width / 2, canvas.height / 2 - 20);
        
        ctx.fillStyle = '#fff';
        ctx.font = '20px Arial';
        ctx.fillText('Press Start to play again', canvas.width / 2, canvas.height / 2 + 20);
    }

    return {
        startGame: startGame,
        togglePause: togglePause,
        stopGame: stopGame
    };
})();

