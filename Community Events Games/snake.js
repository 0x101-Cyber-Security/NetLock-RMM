// Snake Game Implementation
window.snake = (function() {
    let canvas, ctx;
    let gameLoop = null;
    let lastTime = 0;
    let isPaused = false;
    let isGameOver = false;
    
    // Game state
    let score = 0;
    let highScore = 0;
    let speed = 10;
    let foodEaten = 0;
    
    // Grid settings
    const TILE_SIZE = 20;
    const COLS = 30;
    const ROWS = 25;
    const CANVAS_WIDTH = COLS * TILE_SIZE;
    const CANVAS_HEIGHT = ROWS * TILE_SIZE;
    
    // Snake
    let snake = [];
    let direction = { x: 1, y: 0 };
    let nextDirection = { x: 1, y: 0 };
    
    // Food
    let food = { x: 0, y: 0 };
    
    // Special food (appears randomly, worth more points)
    let specialFood = null;
    let specialFoodTimer = 0;
    const specialFoodDuration = 100;
    
    // Game speed
    let moveCounter = 0;
    let moveInterval = 10;
    
    // Controls
    let keys = {};
    
    function init() {
        canvas = document.getElementById('snakeCanvas');
        if (!canvas) {
            console.error('Canvas element not found');
            return false;
        }
        ctx = canvas.getContext('2d');
        
        // Setup keyboard controls
        document.addEventListener('keydown', (e) => {
            keys[e.key] = true;
            
            if (e.key === 'p' || e.key === 'P') {
                togglePause();
                e.preventDefault();
            }
            
            if (!isPaused && !isGameOver) {
                switch(e.key) {
                    case 'ArrowRight':
                        if (direction.x === 0) {
                            nextDirection = { x: 1, y: 0 };
                        }
                        e.preventDefault();
                        break;
                    case 'ArrowLeft':
                        if (direction.x === 0) {
                            nextDirection = { x: -1, y: 0 };
                        }
                        e.preventDefault();
                        break;
                    case 'ArrowDown':
                        if (direction.y === 0) {
                            nextDirection = { x: 0, y: 1 };
                        }
                        e.preventDefault();
                        break;
                    case 'ArrowUp':
                        if (direction.y === 0) {
                            nextDirection = { x: 0, y: -1 };
                        }
                        e.preventDefault();
                        break;
                }
            }
        });
        
        document.addEventListener('keyup', (e) => {
            keys[e.key] = false;
        });
        
        return true;
    }
    
    function startGame() {
        if (!init()) {
            setTimeout(startGame, 100);
            return;
        }
        
        // Reset game state
        score = 0;
        speed = 10;
        foodEaten = 0;
        isPaused = false;
        isGameOver = false;
        moveInterval = 10;
        specialFood = null;
        specialFoodTimer = 0;
        
        // Initialize snake
        snake = [
            { x: 15, y: 12 },
            { x: 14, y: 12 },
            { x: 13, y: 12 }
        ];
        
        direction = { x: 1, y: 0 };
        nextDirection = { x: 1, y: 0 };
        
        // Place first food
        placeFood();
        
        updateScore();
        
        if (gameLoop) {
            cancelAnimationFrame(gameLoop);
        }
        
        lastTime = 0;
        gameLoop = requestAnimationFrame(update);
    }
    
    function placeFood() {
        let validPosition = false;
        
        while (!validPosition) {
            food.x = Math.floor(Math.random() * COLS);
            food.y = Math.floor(Math.random() * ROWS);
            
            // Check if food is on snake
            validPosition = !snake.some(segment => segment.x === food.x && segment.y === food.y);
        }
    }
    
    function placeSpecialFood() {
        let validPosition = false;
        
        while (!validPosition) {
            specialFood = {
                x: Math.floor(Math.random() * COLS),
                y: Math.floor(Math.random() * ROWS)
            };
            
            // Check if special food is on snake or regular food
            validPosition = !snake.some(segment => 
                segment.x === specialFood.x && segment.y === specialFood.y
            ) && !(specialFood.x === food.x && specialFood.y === food.y);
        }
        
        specialFoodTimer = specialFoodDuration;
    }
    
    function update(time = 0) {
        if (isGameOver) {
            drawGameOver();
            return;
        }
        
        const deltaTime = time - lastTime;
        lastTime = time;
        
        if (!isPaused) {
            moveCounter++;
            
            if (moveCounter >= moveInterval) {
                moveCounter = 0;
                
                // Update direction
                direction = { ...nextDirection };
                
                // Calculate new head position
                const head = { ...snake[0] };
                head.x += direction.x;
                head.y += direction.y;
                
                // Check wall collision
                if (head.x < 0 || head.x >= COLS || head.y < 0 || head.y >= ROWS) {
                    isGameOver = true;
                    if (score > highScore) {
                        highScore = score;
                    }
                    return;
                }
                
                // Check self collision
                if (snake.some(segment => segment.x === head.x && segment.y === head.y)) {
                    isGameOver = true;
                    if (score > highScore) {
                        highScore = score;
                    }
                    return;
                }
                
                // Add new head
                snake.unshift(head);
                
                // Check food collision
                if (head.x === food.x && head.y === food.y) {
                    score += 10;
                    foodEaten++;
                    speed = 10 + Math.floor(foodEaten / 5);
                    moveInterval = Math.max(3, 10 - Math.floor(foodEaten / 5));
                    placeFood();
                    
                    // Spawn special food randomly
                    if (Math.random() < 0.2 && !specialFood) {
                        placeSpecialFood();
                    }
                } else if (specialFood && head.x === specialFood.x && head.y === specialFood.y) {
                    // Eat special food
                    score += 50;
                    specialFood = null;
                    specialFoodTimer = 0;
                } else {
                    // Remove tail if no food eaten
                    snake.pop();
                }
            }
            
            // Update special food timer
            if (specialFood) {
                specialFoodTimer--;
                if (specialFoodTimer <= 0) {
                    specialFood = null;
                }
            }
        }
        
        draw();
        updateScore();
        gameLoop = requestAnimationFrame(update);
    }
    
    function draw() {
        // Clear canvas with grid pattern
        ctx.fillStyle = '#1a1a1a';
        ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
        
        // Draw grid
        ctx.strokeStyle = '#2a2a2a';
        ctx.lineWidth = 1;
        for (let x = 0; x <= COLS; x++) {
            ctx.beginPath();
            ctx.moveTo(x * TILE_SIZE, 0);
            ctx.lineTo(x * TILE_SIZE, CANVAS_HEIGHT);
            ctx.stroke();
        }
        for (let y = 0; y <= ROWS; y++) {
            ctx.beginPath();
            ctx.moveTo(0, y * TILE_SIZE);
            ctx.lineTo(CANVAS_WIDTH, y * TILE_SIZE);
            ctx.stroke();
        }
        
        // Draw food
        ctx.fillStyle = '#ff0000';
        ctx.beginPath();
        ctx.arc(
            food.x * TILE_SIZE + TILE_SIZE / 2,
            food.y * TILE_SIZE + TILE_SIZE / 2,
            TILE_SIZE / 2 - 2,
            0,
            Math.PI * 2
        );
        ctx.fill();
        
        // Add shine to food
        ctx.fillStyle = '#ff6666';
        ctx.beginPath();
        ctx.arc(
            food.x * TILE_SIZE + TILE_SIZE / 3,
            food.y * TILE_SIZE + TILE_SIZE / 3,
            TILE_SIZE / 6,
            0,
            Math.PI * 2
        );
        ctx.fill();
        
        // Draw special food
        if (specialFood) {
            const pulse = Math.sin(Date.now() / 100) * 2 + (TILE_SIZE / 2 - 2);
            ctx.fillStyle = '#FFD700';
            ctx.beginPath();
            ctx.arc(
                specialFood.x * TILE_SIZE + TILE_SIZE / 2,
                specialFood.y * TILE_SIZE + TILE_SIZE / 2,
                pulse,
                0,
                Math.PI * 2
            );
            ctx.fill();
            
            // Star effect
            ctx.fillStyle = '#FFF';
            ctx.font = 'bold 16px Arial';
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.fillText('★', specialFood.x * TILE_SIZE + TILE_SIZE / 2, specialFood.y * TILE_SIZE + TILE_SIZE / 2);
        }
        
        // Draw snake
        snake.forEach((segment, index) => {
            if (index === 0) {
                // Head
                ctx.fillStyle = '#00ff00';
            } else {
                // Body - gradient effect
                const alpha = 1 - (index / snake.length) * 0.5;
                ctx.fillStyle = `rgba(0, 200, 0, ${alpha})`;
            }
            
            ctx.fillRect(
                segment.x * TILE_SIZE + 1,
                segment.y * TILE_SIZE + 1,
                TILE_SIZE - 2,
                TILE_SIZE - 2
            );
            
            // Add shine to head
            if (index === 0) {
                ctx.fillStyle = '#66ff66';
                ctx.fillRect(
                    segment.x * TILE_SIZE + 3,
                    segment.y * TILE_SIZE + 3,
                    TILE_SIZE / 3,
                    TILE_SIZE / 3
                );
                
                // Draw eyes based on direction
                ctx.fillStyle = '#000';
                if (direction.x === 1) {
                    // Right
                    ctx.fillRect(segment.x * TILE_SIZE + 12, segment.y * TILE_SIZE + 5, 3, 3);
                    ctx.fillRect(segment.x * TILE_SIZE + 12, segment.y * TILE_SIZE + 12, 3, 3);
                } else if (direction.x === -1) {
                    // Left
                    ctx.fillRect(segment.x * TILE_SIZE + 5, segment.y * TILE_SIZE + 5, 3, 3);
                    ctx.fillRect(segment.x * TILE_SIZE + 5, segment.y * TILE_SIZE + 12, 3, 3);
                } else if (direction.y === 1) {
                    // Down
                    ctx.fillRect(segment.x * TILE_SIZE + 5, segment.y * TILE_SIZE + 12, 3, 3);
                    ctx.fillRect(segment.x * TILE_SIZE + 12, segment.y * TILE_SIZE + 12, 3, 3);
                } else {
                    // Up
                    ctx.fillRect(segment.x * TILE_SIZE + 5, segment.y * TILE_SIZE + 5, 3, 3);
                    ctx.fillRect(segment.x * TILE_SIZE + 12, segment.y * TILE_SIZE + 5, 3, 3);
                }
            }
        });
        
        // Draw pause overlay
        if (isPaused) {
            ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
            ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
            ctx.fillStyle = '#fff';
            ctx.font = '40px Arial';
            ctx.textAlign = 'center';
            ctx.fillText('PAUSED', CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2);
        }
    }
    
    function drawGameOver() {
        ctx.fillStyle = 'rgba(0, 0, 0, 0.8)';
        ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
        
        ctx.fillStyle = '#f00';
        ctx.font = 'bold 50px Arial';
        ctx.textAlign = 'center';
        ctx.fillText('GAME OVER', CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2 - 60);
        
        ctx.fillStyle = '#fff';
        ctx.font = '24px Arial';
        ctx.fillText('Score: ' + score, CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2 - 10);
        
        if (score === highScore && score > 0) {
            ctx.fillStyle = '#FFD700';
            ctx.font = 'bold 20px Arial';
            ctx.fillText('🏆 NEW HIGH SCORE! 🏆', CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2 + 25);
        }
        
        ctx.fillStyle = '#aaa';
        ctx.font = '20px Arial';
        ctx.fillText('Press Start to play again', CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2 + 60);
    }
    
    function updateScore() {
        document.getElementById('snake-score').textContent = score;
        document.getElementById('snake-highscore').textContent = highScore;
        document.getElementById('snake-length').textContent = snake.length;
        document.getElementById('snake-speed').textContent = speed;
        
        // Update mobile
        const scoreMobile = document.getElementById('snake-score-mobile');
        const highscoreMobile = document.getElementById('snake-highscore-mobile');
        const lengthMobile = document.getElementById('snake-length-mobile');
        const speedMobile = document.getElementById('snake-speed-mobile');
        
        if (scoreMobile) scoreMobile.textContent = score;
        if (highscoreMobile) highscoreMobile.textContent = highScore;
        if (lengthMobile) lengthMobile.textContent = snake.length;
        if (speedMobile) speedMobile.textContent = speed;
    }
    
    function togglePause() {
        if (isGameOver || !gameLoop) {
            return;
        }
        isPaused = !isPaused;
    }
    
    function stopGame() {
        if (gameLoop) {
            cancelAnimationFrame(gameLoop);
            gameLoop = null;
        }
        
        snake = [];
        isPaused = false;
        isGameOver = true;
        
        // Draw stopped screen
        ctx.fillStyle = '#1a1a1a';
        ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
        
        ctx.fillStyle = 'rgba(0, 0, 0, 0.8)';
        ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
        
        ctx.fillStyle = '#ff9800';
        ctx.font = 'bold 50px Arial';
        ctx.textAlign = 'center';
        ctx.fillText('STOPPED', CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2 - 20);
        
        ctx.fillStyle = '#fff';
        ctx.font = '20px Arial';
        ctx.fillText('Press Start to play again', CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2 + 30);
    }
    
    return {
        startGame: startGame,
        togglePause: togglePause,
        stopGame: stopGame
    };
})();

