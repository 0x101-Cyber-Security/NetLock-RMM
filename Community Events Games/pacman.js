// Pac-Man Game Implementation
window.pacman = (function() {
    let canvas, ctx;
    let gameLoop = null;
    let lastTime = 0;
    let isPaused = false;
    let isGameOver = false;
    
    // Game state
    let score = 0;
    let lives = 3;
    let level = 1;
    let pelletsEaten = 0;
    let totalPellets = 0;
    
    // Grid settings
    const TILE_SIZE = 20;
    const COLS = 28;
    const ROWS = 31;
    const CANVAS_WIDTH = COLS * TILE_SIZE;
    const CANVAS_HEIGHT = ROWS * TILE_SIZE;
    
    // Pac-Man
    let pacman = {
        x: 14,
        y: 23,
        direction: 0, // 0: right, 1: down, 2: left, 3: up
        nextDirection: 0,
        speed: 0.15,
        mouthOpen: 0,
        mouthSpeed: 0.3
    };
    
    // Ghosts
    let ghosts = [];
    const ghostColors = ['#FF0000', '#FFB8FF', '#00FFFF', '#FFB852'];
    const ghostNames = ['Blinky', 'Pinky', 'Inky', 'Clyde'];
    
    // Power mode
    let powerMode = false;
    let powerModeTimer = 0;
    const powerModeDuration = 200;
    
    // Controls
    let keys = {};
    
    // Maze (1 = wall, 0 = empty, 2 = pellet, 3 = power pellet)
    let maze = [];
    
    // Direction vectors
    const directions = [
        { x: 1, y: 0 },   // right
        { x: 0, y: 1 },   // down
        { x: -1, y: 0 },  // left
        { x: 0, y: -1 }   // up
    ];
    
    function init() {
        canvas = document.getElementById('pacmanCanvas');
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
                        pacman.nextDirection = 0;
                        e.preventDefault();
                        break;
                    case 'ArrowDown':
                        pacman.nextDirection = 1;
                        e.preventDefault();
                        break;
                    case 'ArrowLeft':
                        pacman.nextDirection = 2;
                        e.preventDefault();
                        break;
                    case 'ArrowUp':
                        pacman.nextDirection = 3;
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
    
    function createMaze() {
        // Classic Pac-Man inspired maze
        maze = [
            [1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1],
            [1,2,2,2,2,2,2,2,2,2,2,2,2,1,1,2,2,2,2,2,2,2,2,2,2,2,2,1],
            [1,2,1,1,1,1,2,1,1,1,1,1,2,1,1,2,1,1,1,1,1,2,1,1,1,1,2,1],
            [1,3,1,1,1,1,2,1,1,1,1,1,2,1,1,2,1,1,1,1,1,2,1,1,1,1,3,1],
            [1,2,1,1,1,1,2,1,1,1,1,1,2,1,1,2,1,1,1,1,1,2,1,1,1,1,2,1],
            [1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1],
            [1,2,1,1,1,1,2,1,1,2,1,1,1,1,1,1,1,1,2,1,1,2,1,1,1,1,2,1],
            [1,2,1,1,1,1,2,1,1,2,1,1,1,1,1,1,1,1,2,1,1,2,1,1,1,1,2,1],
            [1,2,2,2,2,2,2,1,1,2,2,2,2,1,1,2,2,2,2,1,1,2,2,2,2,2,2,1],
            [1,1,1,1,1,1,2,1,1,1,1,1,0,1,1,0,1,1,1,1,1,2,1,1,1,1,1,1],
            [1,1,1,1,1,1,2,1,1,1,1,1,0,1,1,0,1,1,1,1,1,2,1,1,1,1,1,1],
            [1,1,1,1,1,1,2,1,1,0,0,0,0,0,0,0,0,0,0,1,1,2,1,1,1,1,1,1],
            [1,1,1,1,1,1,2,1,1,0,1,1,1,0,0,1,1,1,0,1,1,2,1,1,1,1,1,1],
            [1,1,1,1,1,1,2,1,1,0,1,0,0,0,0,0,0,1,0,1,1,2,1,1,1,1,1,1],
            [0,0,0,0,0,0,2,0,0,0,1,0,0,0,0,0,0,1,0,0,0,2,0,0,0,0,0,0],
            [1,1,1,1,1,1,2,1,1,0,1,0,0,0,0,0,0,1,0,1,1,2,1,1,1,1,1,1],
            [1,1,1,1,1,1,2,1,1,0,1,1,1,1,1,1,1,1,0,1,1,2,1,1,1,1,1,1],
            [1,1,1,1,1,1,2,1,1,0,0,0,0,0,0,0,0,0,0,1,1,2,1,1,1,1,1,1],
            [1,1,1,1,1,1,2,1,1,0,1,1,1,1,1,1,1,1,0,1,1,2,1,1,1,1,1,1],
            [1,1,1,1,1,1,2,1,1,0,1,1,1,1,1,1,1,1,0,1,1,2,1,1,1,1,1,1],
            [1,2,2,2,2,2,2,2,2,2,2,2,2,1,1,2,2,2,2,2,2,2,2,2,2,2,2,1],
            [1,2,1,1,1,1,2,1,1,1,1,1,2,1,1,2,1,1,1,1,1,2,1,1,1,1,2,1],
            [1,2,1,1,1,1,2,1,1,1,1,1,2,1,1,2,1,1,1,1,1,2,1,1,1,1,2,1],
            [1,3,2,2,1,1,2,2,2,2,2,2,2,0,0,2,2,2,2,2,2,2,1,1,2,2,3,1],
            [1,1,1,2,1,1,2,1,1,2,1,1,1,1,1,1,1,1,2,1,1,2,1,1,2,1,1,1],
            [1,1,1,2,1,1,2,1,1,2,1,1,1,1,1,1,1,1,2,1,1,2,1,1,2,1,1,1],
            [1,2,2,2,2,2,2,1,1,2,2,2,2,1,1,2,2,2,2,1,1,2,2,2,2,2,2,1],
            [1,2,1,1,1,1,1,1,1,1,1,1,2,1,1,2,1,1,1,1,1,1,1,1,1,1,2,1],
            [1,2,1,1,1,1,1,1,1,1,1,1,2,1,1,2,1,1,1,1,1,1,1,1,1,1,2,1],
            [1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1],
            [1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1]
        ];
        
        // Count pellets
        totalPellets = 0;
        for (let y = 0; y < ROWS; y++) {
            for (let x = 0; x < COLS; x++) {
                if (maze[y][x] === 2 || maze[y][x] === 3) {
                    totalPellets++;
                }
            }
        }
    }
    
    function startGame() {
        if (!init()) {
            setTimeout(startGame, 100);
            return;
        }
        
        // Reset game state
        score = 0;
        lives = 3;
        level = 1;
        pelletsEaten = 0;
        isPaused = false;
        isGameOver = false;
        powerMode = false;
        powerModeTimer = 0;
        
        createMaze();
        
        // Initialize Pac-Man
        pacman.x = 14;
        pacman.y = 23;
        pacman.direction = 0;
        pacman.nextDirection = 0;
        pacman.mouthOpen = 0;
        
        // Initialize ghosts
        ghosts = [];
        const startPositions = [
            { x: 13, y: 11 },
            { x: 14, y: 11 },
            { x: 13, y: 14 },
            { x: 14, y: 14 }
        ];
        
        for (let i = 0; i < 4; i++) {
            ghosts.push({
                x: startPositions[i].x,
                y: startPositions[i].y,
                direction: Math.floor(Math.random() * 4),
                color: ghostColors[i],
                name: ghostNames[i],
                speed: 0.1 - i * 0.01,
                scared: false
            });
        }
        
        updateScore();
        
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
            // Update power mode
            if (powerMode) {
                powerModeTimer--;
                if (powerModeTimer <= 0) {
                    powerMode = false;
                    ghosts.forEach(ghost => ghost.scared = false);
                }
            }
            
            // Try to change direction
            const nextDir = directions[pacman.nextDirection];
            const nextX = pacman.x + nextDir.x * pacman.speed;
            const nextY = pacman.y + nextDir.y * pacman.speed;
            
            if (canMove(nextX, nextY)) {
                pacman.direction = pacman.nextDirection;
            }
            
            // Move Pac-Man
            const dir = directions[pacman.direction];
            let newX = pacman.x + dir.x * pacman.speed;
            let newY = pacman.y + dir.y * pacman.speed;
            
            // Tunnel wrap
            if (newX < 0) newX = COLS - 1;
            if (newX >= COLS) newX = 0;
            
            if (canMove(newX, newY)) {
                pacman.x = newX;
                pacman.y = newY;
            }
            
            // Animate mouth
            pacman.mouthOpen += pacman.mouthSpeed;
            if (pacman.mouthOpen > 1) {
                pacman.mouthOpen = 0;
            }
            
            // Check pellet collision
            const tileX = Math.round(pacman.x);
            const tileY = Math.round(pacman.y);
            
            if (maze[tileY] && maze[tileY][tileX] === 2) {
                maze[tileY][tileX] = 0;
                score += 10;
                pelletsEaten++;
            } else if (maze[tileY] && maze[tileY][tileX] === 3) {
                maze[tileY][tileX] = 0;
                score += 50;
                pelletsEaten++;
                powerMode = true;
                powerModeTimer = powerModeDuration;
                ghosts.forEach(ghost => ghost.scared = true);
            }
            
            // Check win condition
            if (pelletsEaten >= totalPellets) {
                level++;
                pelletsEaten = 0;
                createMaze();
                pacman.x = 14;
                pacman.y = 23;
                pacman.speed = Math.min(0.25, 0.15 + level * 0.02);
                ghosts.forEach(ghost => {
                    ghost.speed = Math.min(0.18, 0.1 + level * 0.015);
                });
            }
            
            // Move ghosts
            ghosts.forEach(ghost => {
                if (Math.random() < 0.02) {
                    ghost.direction = Math.floor(Math.random() * 4);
                }
                
                const ghostDir = directions[ghost.direction];
                let ghostNewX = ghost.x + ghostDir.x * ghost.speed;
                let ghostNewY = ghost.y + ghostDir.y * ghost.speed;
                
                // Tunnel wrap
                if (ghostNewX < 0) ghostNewX = COLS - 1;
                if (ghostNewX >= COLS) ghostNewX = 0;
                
                if (canMove(ghostNewX, ghostNewY)) {
                    ghost.x = ghostNewX;
                    ghost.y = ghostNewY;
                } else {
                    ghost.direction = Math.floor(Math.random() * 4);
                }
            });
            
            // Check ghost collision
            ghosts.forEach((ghost, index) => {
                const dist = Math.sqrt(
                    Math.pow(pacman.x - ghost.x, 2) + 
                    Math.pow(pacman.y - ghost.y, 2)
                );
                
                if (dist < 0.6) {
                    if (powerMode && ghost.scared) {
                        // Eat ghost
                        score += 200;
                        ghost.x = 14;
                        ghost.y = 14;
                        ghost.scared = false;
                    } else if (!ghost.scared) {
                        // Lose life
                        lives--;
                        if (lives <= 0) {
                            isGameOver = true;
                        } else {
                            // Reset positions
                            pacman.x = 14;
                            pacman.y = 23;
                            ghosts.forEach((g, i) => {
                                g.x = 13 + (i % 2);
                                g.y = 11 + Math.floor(i / 2) * 3;
                            });
                        }
                    }
                }
            });
        }
        
        draw();
        updateScore();
        gameLoop = requestAnimationFrame(update);
    }
    
    function canMove(x, y) {
        const tileX = Math.round(x);
        const tileY = Math.round(y);
        
        if (tileY < 0 || tileY >= ROWS || tileX < 0 || tileX >= COLS) {
            return false;
        }
        
        return maze[tileY][tileX] !== 1;
    }
    
    function draw() {
        // Clear canvas
        ctx.fillStyle = '#000';
        ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
        
        // Draw maze
        for (let y = 0; y < ROWS; y++) {
            for (let x = 0; x < COLS; x++) {
                const tile = maze[y][x];
                
                if (tile === 1) {
                    // Wall
                    ctx.fillStyle = '#2121DE';
                    ctx.fillRect(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE);
                    ctx.strokeStyle = '#1919B4';
                    ctx.strokeRect(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE);
                } else if (tile === 2) {
                    // Pellet
                    ctx.fillStyle = '#FFB8AE';
                    ctx.beginPath();
                    ctx.arc(
                        x * TILE_SIZE + TILE_SIZE / 2,
                        y * TILE_SIZE + TILE_SIZE / 2,
                        2,
                        0,
                        Math.PI * 2
                    );
                    ctx.fill();
                } else if (tile === 3) {
                    // Power pellet
                    ctx.fillStyle = '#FFB8AE';
                    const pulse = Math.sin(Date.now() / 200) * 2 + 4;
                    ctx.beginPath();
                    ctx.arc(
                        x * TILE_SIZE + TILE_SIZE / 2,
                        y * TILE_SIZE + TILE_SIZE / 2,
                        pulse,
                        0,
                        Math.PI * 2
                    );
                    ctx.fill();
                }
            }
        }
        
        // Draw ghosts
        ghosts.forEach(ghost => {
            const x = ghost.x * TILE_SIZE;
            const y = ghost.y * TILE_SIZE;
            
            if (ghost.scared) {
                ctx.fillStyle = '#2121DE';
            } else {
                ctx.fillStyle = ghost.color;
            }
            
            // Ghost body
            ctx.beginPath();
            ctx.arc(x + TILE_SIZE / 2, y + TILE_SIZE / 2, TILE_SIZE / 2 - 2, Math.PI, 0);
            ctx.lineTo(x + TILE_SIZE - 2, y + TILE_SIZE);
            ctx.lineTo(x + TILE_SIZE - 5, y + TILE_SIZE - 4);
            ctx.lineTo(x + TILE_SIZE / 2 + 2, y + TILE_SIZE);
            ctx.lineTo(x + TILE_SIZE / 2 - 2, y + TILE_SIZE - 4);
            ctx.lineTo(x + 5, y + TILE_SIZE);
            ctx.lineTo(x + 2, y + TILE_SIZE);
            ctx.closePath();
            ctx.fill();
            
            // Eyes
            ctx.fillStyle = '#FFF';
            ctx.fillRect(x + 5, y + 7, 4, 6);
            ctx.fillRect(x + TILE_SIZE - 9, y + 7, 4, 6);
            
            if (!ghost.scared) {
                ctx.fillStyle = '#000';
                ctx.fillRect(x + 6, y + 9, 2, 3);
                ctx.fillRect(x + TILE_SIZE - 8, y + 9, 2, 3);
            }
        });
        
        // Draw Pac-Man
        const pacX = pacman.x * TILE_SIZE;
        const pacY = pacman.y * TILE_SIZE;
        
        ctx.fillStyle = '#FFFF00';
        ctx.beginPath();
        
        const mouthAngle = Math.abs(Math.sin(pacman.mouthOpen * Math.PI)) * 0.4;
        const rotation = pacman.direction * Math.PI / 2;
        
        ctx.arc(
            pacX + TILE_SIZE / 2,
            pacY + TILE_SIZE / 2,
            TILE_SIZE / 2 - 2,
            rotation + mouthAngle,
            rotation + Math.PI * 2 - mouthAngle
        );
        ctx.lineTo(pacX + TILE_SIZE / 2, pacY + TILE_SIZE / 2);
        ctx.fill();
        
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
        ctx.fillText('GAME OVER', CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2 - 40);
        
        ctx.fillStyle = '#fff';
        ctx.font = '24px Arial';
        ctx.fillText('Final Score: ' + score, CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2 + 10);
        ctx.font = '20px Arial';
        ctx.fillText('Press Start to play again', CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2 + 50);
    }
    
    function updateScore() {
        document.getElementById('pm-score').textContent = score;
        document.getElementById('pm-level').textContent = level;
        document.getElementById('pm-lives').textContent = lives;
        document.getElementById('pm-pellets').textContent = pelletsEaten + ' / ' + totalPellets;
        
        // Update mobile
        const scoreMobile = document.getElementById('pm-score-mobile');
        const levelMobile = document.getElementById('pm-level-mobile');
        const livesMobile = document.getElementById('pm-lives-mobile');
        const pelletsMobile = document.getElementById('pm-pellets-mobile');
        
        if (scoreMobile) scoreMobile.textContent = score;
        if (levelMobile) levelMobile.textContent = level;
        if (livesMobile) livesMobile.textContent = lives;
        if (pelletsMobile) pelletsMobile.textContent = pelletsEaten + ' / ' + totalPellets;
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
        
        isPaused = false;
        isGameOver = true;
        
        // Draw stopped screen
        ctx.fillStyle = '#000';
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

