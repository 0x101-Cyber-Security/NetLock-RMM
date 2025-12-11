// Space Invaders Game Implementation
window.spaceInvaders = (function() {
    let canvas, ctx;
    let gameLoop = null;
    let lastTime = 0;
    let isPaused = false;
    let isGameOver = false;
    
    // Game state
    let score = 0;
    let lives = 3;
    let level = 1;
    let kills = 0;
    let combo = 0;
    let maxCombo = 0;
    let comboTimer = 0;
    const comboTimeout = 100;
    
    // Player
    let player = {
        x: 0,
        y: 0,
        width: 40,
        height: 30,
        speed: 6,
        color: '#00ff00',
        invulnerable: 0
    };
    
    // Bullets
    let bullets = [];
    const bulletSpeed = 8;
    const bulletWidth = 4;
    const bulletHeight = 15;
    let canShoot = true;
    let shootCooldown = 0;
    const shootCooldownMax = 10;
    
    // Shields
    let shields = [];
    
    // Aliens
    let aliens = [];
    let alienDirection = 1;
    let alienSpeed = 2; // Increased base speed
    let alienDropDistance = 20;
    let shouldDropAliens = false;
    
    // Alien bullets
    let alienBullets = [];
    const alienBulletSpeed = 3;
    let alienShootChance = 0.001;
    
    // Controls
    let keys = {};
    
    // Canvas dimensions
    const CANVAS_WIDTH = 600;
    const CANVAS_HEIGHT = 600;
    
    function init() {
        canvas = document.getElementById('spaceInvadersCanvas');
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
            
            if (e.key === ' ' && !isPaused && !isGameOver) {
                shoot();
                e.preventDefault();
            }
        });
        
        document.addEventListener('keyup', (e) => {
            keys[e.key] = false;
        });
        
        return true;
    }
    
    function createShields() {
        shields = [];
        const shieldWidth = 80;
        const shieldHeight = 60;
        const shieldY = CANVAS_HEIGHT - 150;
        const spacing = (CANVAS_WIDTH - 4 * shieldWidth) / 5;
        
        for (let i = 0; i < 4; i++) {
            const shield = {
                x: spacing + i * (shieldWidth + spacing),
                y: shieldY,
                width: shieldWidth,
                height: shieldHeight,
                blocks: []
            };
            
            // Create shield blocks (8x6 grid)
            for (let row = 0; row < 6; row++) {
                for (let col = 0; col < 8; col++) {
                    // Skip corners for classic shield shape
                    if ((row < 2 && (col < 2 || col > 5)) || 
                        (row === 5 && col > 2 && col < 5)) {
                        continue;
                    }
                    shield.blocks.push({
                        x: col * 10,
                        y: row * 10,
                        active: true
                    });
                }
            }
            shields.push(shield);
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
        kills = 0;
        combo = 0;
        maxCombo = 0;
        comboTimer = 0;
        isPaused = false;
        isGameOver = false;
        bullets = [];
        alienBullets = [];
        canShoot = true;
        shootCooldown = 0;
        
        // Initialize player
        player.x = CANVAS_WIDTH / 2 - player.width / 2;
        player.y = CANVAS_HEIGHT - player.height - 40;
        player.invulnerable = 0;
        
        // Create aliens and shields
        createAliens();
        createShields();
        
        updateScore();
        
        if (gameLoop) {
            cancelAnimationFrame(gameLoop);
        }
        
        lastTime = 0;
        gameLoop = requestAnimationFrame(update);
    }
    
    function createAliens() {
        aliens = [];
        const rows = 4 + Math.floor(level / 2);
        const cols = 8;
        const alienWidth = 40;
        const alienHeight = 30;
        const padding = 10;
        const offsetX = 80;
        const offsetY = 60;
        
        // Faster speed progression
        alienSpeed = 2 + (level - 1) * 0.5;
        alienShootChance = 0.001 + (level - 1) * 0.0002;
        
        for (let row = 0; row < rows; row++) {
            for (let col = 0; col < cols; col++) {
                aliens.push({
                    x: offsetX + col * (alienWidth + padding),
                    y: offsetY + row * (alienHeight + padding),
                    width: alienWidth,
                    height: alienHeight,
                    alive: true,
                    type: row % 3
                });
            }
        }
    }
    
    function update(time = 0) {
        if (isGameOver) {
            drawGameOver();
            return;
        }
        
        const deltaTime = time - lastTime;
        lastTime = time;
        
        if (!isPaused) {
            // Update timers
            if (player.invulnerable > 0) player.invulnerable--;
            if (shootCooldown > 0) shootCooldown--;
            if (comboTimer > 0) {
                comboTimer--;
            } else if (combo > 0) {
                combo = 0;
            }
            
            // Move player
            if (keys['ArrowLeft'] && player.x > 0) {
                player.x -= player.speed;
            }
            if (keys['ArrowRight'] && player.x < CANVAS_WIDTH - player.width) {
                player.x += player.speed;
            }
            
            // Move bullets
            for (let i = bullets.length - 1; i >= 0; i--) {
                bullets[i].y -= bulletSpeed;
                if (bullets[i].y < 0) {
                    bullets.splice(i, 1);
                }
            }
            
            // Move alien bullets
            for (let i = alienBullets.length - 1; i >= 0; i--) {
                alienBullets[i].y += alienBulletSpeed;
                if (alienBullets[i].y > CANVAS_HEIGHT) {
                    alienBullets.splice(i, 1);
                }
            }
            
            // Move aliens (smooth movement every frame)
            moveAliens();
            
            // Aliens shoot (only from bottom row aliens)
            const aliveAliens = aliens.filter(a => a.alive);
            const bottomAliens = aliveAliens.filter(alien => {
                return !aliveAliens.some(other => 
                    other.alive && 
                    Math.abs(other.x - alien.x) < 30 && 
                    other.y > alien.y
                );
            });
            
            bottomAliens.forEach(alien => {
                if (Math.random() < alienShootChance) {
                    alienBullets.push({
                        x: alien.x + alien.width / 2,
                        y: alien.y + alien.height,
                        width: 4,
                        height: 12
                    });
                }
            });
            
            // Check collisions
            checkCollisions();
            
            // Check win condition
            if (aliens.every(alien => !alien.alive)) {
                level++;
                kills = 0;
                score += level * 100; // Bonus for completing level
                createAliens();
                createShields(); // Regenerate shields
            }
            
            // Check game over
            if (lives <= 0) {
                isGameOver = true;
            }
            
            // Check if aliens reached player
            aliens.forEach(alien => {
                if (alien.alive && alien.y + alien.height >= player.y) {
                    lives = 0;
                    isGameOver = true;
                }
            });
        }
        
        draw();
        updateScore();
        gameLoop = requestAnimationFrame(update);
    }
    
    function moveAliens() {
        // Check if any alien will hit the edge in the next move
        let willHitEdge = false;
        
        aliens.forEach(alien => {
            if (alien.alive) {
                const nextX = alien.x + alienDirection * alienSpeed;
                if ((alienDirection > 0 && nextX + alien.width >= CANVAS_WIDTH - 10) ||
                    (alienDirection < 0 && nextX <= 10)) {
                    willHitEdge = true;
                }
            }
        });
        
        if (willHitEdge) {
            // Change direction and drop
            alienDirection *= -1;
            aliens.forEach(alien => {
                if (alien.alive) {
                    alien.y += alienDropDistance;
                }
            });
        }
        
        // Move aliens horizontally (smooth continuous movement)
        aliens.forEach(alien => {
            if (alien.alive) {
                alien.x += alienDirection * alienSpeed;
            }
        });
    }
    
    function shoot() {
        if (shootCooldown > 0 || isPaused || isGameOver) {
            return;
        }
        
        bullets.push({
            x: player.x + player.width / 2 - bulletWidth / 2,
            y: player.y,
            width: bulletWidth,
            height: bulletHeight
        });
        
        shootCooldown = shootCooldownMax;
    }
    
    function checkCollisions() {
        // Bullet vs Aliens
        for (let bulletIndex = bullets.length - 1; bulletIndex >= 0; bulletIndex--) {
            const bullet = bullets[bulletIndex];
            let bulletDestroyed = false;
            
            // Check shields
            for (let shield of shields) {
                if (bulletDestroyed) break;
                for (let i = shield.blocks.length - 1; i >= 0; i--) {
                    const block = shield.blocks[i];
                    if (!block.active) continue;
                    
                    const blockX = shield.x + block.x;
                    const blockY = shield.y + block.y;
                    
                    if (bullet.x < blockX + 10 &&
                        bullet.x + bullet.width > blockX &&
                        bullet.y < blockY + 10 &&
                        bullet.y + bullet.height > blockY) {
                        
                        block.active = false;
                        bullets.splice(bulletIndex, 1);
                        bulletDestroyed = true;
                        break;
                    }
                }
            }
            
            if (bulletDestroyed) continue;
            
            // Check aliens
            for (let alienIndex = 0; alienIndex < aliens.length; alienIndex++) {
                const alien = aliens[alienIndex];
                if (!alien.alive) continue;
                
                if (bullet.x < alien.x + alien.width &&
                    bullet.x + bullet.width > alien.x &&
                    bullet.y < alien.y + alien.height &&
                    bullet.y + bullet.height > alien.y) {
                    
                    alien.alive = false;
                    bullets.splice(bulletIndex, 1);
                    
                    // Combo system
                    combo++;
                    comboTimer = comboTimeout;
                    if (combo > maxCombo) maxCombo = combo;
                    
                    const baseScore = (3 - alien.type) * 10 * level;
                    const comboBonus = Math.floor(baseScore * (combo - 1) * 0.5);
                    score += baseScore + comboBonus;
                    kills++;
                    break;
                }
            }
        }
        
        // Alien bullets vs Shields
        for (let bulletIndex = alienBullets.length - 1; bulletIndex >= 0; bulletIndex--) {
            const bullet = alienBullets[bulletIndex];
            let bulletDestroyed = false;
            
            for (let shield of shields) {
                if (bulletDestroyed) break;
                for (let i = shield.blocks.length - 1; i >= 0; i--) {
                    const block = shield.blocks[i];
                    if (!block.active) continue;
                    
                    const blockX = shield.x + block.x;
                    const blockY = shield.y + block.y;
                    
                    if (bullet.x < blockX + 10 &&
                        bullet.x + bullet.width > blockX &&
                        bullet.y < blockY + 10 &&
                        bullet.y + bullet.height > blockY) {
                        
                        block.active = false;
                        alienBullets.splice(bulletIndex, 1);
                        bulletDestroyed = true;
                        break;
                    }
                }
            }
            
            if (bulletDestroyed) continue;
            
            // Check player
            if (player.invulnerable === 0 &&
                bullet.x < player.x + player.width &&
                bullet.x + bullet.width > player.x &&
                bullet.y < player.y + player.height &&
                bullet.y + bullet.height > player.y) {
                
                alienBullets.splice(bulletIndex, 1);
                lives--;
                player.invulnerable = 60; // 1 second invulnerability
                combo = 0; // Reset combo on hit
            }
        }
    }
    
    function draw() {
        // Clear canvas
        ctx.fillStyle = '#000';
        ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
        
        // Draw animated stars background
        ctx.fillStyle = '#fff';
        for (let i = 0; i < 100; i++) {
            const x = (i * 123 + Date.now() / 100) % CANVAS_WIDTH;
            const y = (i * 456) % CANVAS_HEIGHT;
            const size = (i % 3) + 1;
            ctx.fillRect(x, y, size, size);
        }
        
        // Draw shields
        shields.forEach(shield => {
            shield.blocks.forEach(block => {
                if (block.active) {
                    ctx.fillStyle = '#00ff00';
                    ctx.fillRect(
                        shield.x + block.x,
                        shield.y + block.y,
                        10,
                        10
                    );
                }
            });
        });
        
        // Draw player (with invulnerability flash)
        if (player.invulnerable === 0 || Math.floor(player.invulnerable / 5) % 2 === 0) {
            drawPlayer();
        }
        
        // Draw bullets with glow
        bullets.forEach(bullet => {
            // Glow
            ctx.fillStyle = 'rgba(255, 255, 0, 0.3)';
            ctx.fillRect(bullet.x - 2, bullet.y - 2, bullet.width + 4, bullet.height + 4);
            // Bullet
            ctx.fillStyle = '#ffff00';
            ctx.fillRect(bullet.x, bullet.y, bullet.width, bullet.height);
        });
        
        // Draw alien bullets with glow
        alienBullets.forEach(bullet => {
            // Glow
            ctx.fillStyle = 'rgba(255, 0, 0, 0.3)';
            ctx.fillRect(bullet.x - 2, bullet.y - 2, bullet.width + 4, bullet.height + 4);
            // Bullet
            ctx.fillStyle = '#ff0000';
            ctx.fillRect(bullet.x, bullet.y, bullet.width, bullet.height);
        });
        
        // Draw aliens
        aliens.forEach(alien => {
            if (alien.alive) {
                drawAlien(alien);
            }
        });
        
        // Draw combo indicator
        if (combo > 1) {
            ctx.fillStyle = '#FFD700';
            ctx.font = 'bold 24px Arial';
            ctx.textAlign = 'left';
            ctx.fillText(`COMBO x${combo}!`, 10, 30);
            
            // Combo bar
            const barWidth = 200;
            const barHeight = 10;
            const fillWidth = (comboTimer / comboTimeout) * barWidth;
            
            ctx.fillStyle = 'rgba(0, 0, 0, 0.5)';
            ctx.fillRect(10, 40, barWidth, barHeight);
            ctx.fillStyle = '#FFD700';
            ctx.fillRect(10, 40, fillWidth, barHeight);
        }
        
        // Draw shoot cooldown indicator
        if (shootCooldown > 0) {
            const barWidth = 40;
            const barHeight = 5;
            const fillWidth = (shootCooldown / shootCooldownMax) * barWidth;
            
            ctx.fillStyle = 'rgba(255, 0, 0, 0.5)';
            ctx.fillRect(player.x, player.y - 10, barWidth, barHeight);
            ctx.fillStyle = '#00ff00';
            ctx.fillRect(player.x, player.y - 10, barWidth - fillWidth, barHeight);
        }
        
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
    
    function drawPlayer() {
        // Draw glow effect
        ctx.fillStyle = 'rgba(0, 255, 0, 0.2)';
        ctx.beginPath();
        ctx.arc(player.x + player.width / 2, player.y + player.height / 2, player.width / 1.5, 0, Math.PI * 2);
        ctx.fill();
        
        // Draw spaceship body
        ctx.fillStyle = player.color;
        ctx.beginPath();
        ctx.moveTo(player.x + player.width / 2, player.y);
        ctx.lineTo(player.x + player.width, player.y + player.height);
        ctx.lineTo(player.x, player.y + player.height);
        ctx.closePath();
        ctx.fill();
        
        // Draw wings
        ctx.fillStyle = '#00cc00';
        ctx.fillRect(player.x, player.y + player.height - 10, 10, 10);
        ctx.fillRect(player.x + player.width - 10, player.y + player.height - 10, 10, 10);
        
        // Draw cockpit
        ctx.fillStyle = '#00ffff';
        ctx.beginPath();
        ctx.arc(player.x + player.width / 2, player.y + 12, 6, 0, Math.PI * 2);
        ctx.fill();
        
        // Draw engines
        const engineGlow = Math.sin(Date.now() / 50) * 5 + 10;
        ctx.fillStyle = `rgba(255, 100, 0, 0.8)`;
        ctx.fillRect(player.x + 8, player.y + player.height, 8, engineGlow);
        ctx.fillRect(player.x + player.width - 16, player.y + player.height, 8, engineGlow);
    }
    
    function drawAlien(alien) {
        const colors = ['#ff00ff', '#00ffff', '#ffff00'];
        const glowColors = ['rgba(255, 0, 255, 0.3)', 'rgba(0, 255, 255, 0.3)', 'rgba(255, 255, 0, 0.3)'];
        
        // Draw glow
        ctx.fillStyle = glowColors[alien.type];
        ctx.beginPath();
        ctx.arc(alien.x + alien.width / 2, alien.y + alien.height / 2, alien.width / 1.8, 0, Math.PI * 2);
        ctx.fill();
        
        // Draw alien body with animation
        const wobble = Math.sin(Date.now() / 200 + alien.x) * 2;
        ctx.fillStyle = colors[alien.type];
        ctx.fillRect(alien.x + 5, alien.y + 5 + wobble, alien.width - 10, alien.height - 10);
        
        // Draw alien eyes (animated)
        const eyeBlink = Math.floor(Date.now() / 2000) % 10 === 0 ? 2 : 6;
        ctx.fillStyle = '#fff';
        ctx.fillRect(alien.x + 10, alien.y + 10, 6, eyeBlink);
        ctx.fillRect(alien.x + alien.width - 16, alien.y + 10, 6, eyeBlink);
        
        // Draw pupils
        if (eyeBlink === 6) {
            ctx.fillStyle = '#000';
            ctx.fillRect(alien.x + 12, alien.y + 12, 2, 2);
            ctx.fillRect(alien.x + alien.width - 14, alien.y + 12, 2, 2);
        }
        
        // Draw alien antennae with animated tips
        const antennaeGlow = Math.sin(Date.now() / 100 + alien.x) * 2;
        ctx.strokeStyle = colors[alien.type];
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(alien.x + 10, alien.y + 5);
        ctx.lineTo(alien.x + 5, alien.y);
        ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(alien.x + alien.width - 10, alien.y + 5);
        ctx.lineTo(alien.x + alien.width - 5, alien.y);
        ctx.stroke();
        
        // Antennae tips
        ctx.fillStyle = colors[alien.type];
        ctx.beginPath();
        ctx.arc(alien.x + 5, alien.y - antennaeGlow, 3, 0, Math.PI * 2);
        ctx.fill();
        ctx.beginPath();
        ctx.arc(alien.x + alien.width - 5, alien.y - antennaeGlow, 3, 0, Math.PI * 2);
        ctx.fill();
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
        document.getElementById('si-score').textContent = score;
        document.getElementById('si-level').textContent = level;
        document.getElementById('si-lives').textContent = lives;
        document.getElementById('si-kills').textContent = kills;
        document.getElementById('si-combo').textContent = maxCombo;
        
        // Update mobile
        const scoreMobile = document.getElementById('si-score-mobile');
        const levelMobile = document.getElementById('si-level-mobile');
        const livesMobile = document.getElementById('si-lives-mobile');
        const killsMobile = document.getElementById('si-kills-mobile');
        const comboMobile = document.getElementById('si-combo-mobile');
        
        if (scoreMobile) scoreMobile.textContent = score;
        if (levelMobile) levelMobile.textContent = level;
        if (livesMobile) livesMobile.textContent = lives;
        if (killsMobile) killsMobile.textContent = kills;
        if (comboMobile) comboMobile.textContent = maxCombo;
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
        
        aliens = [];
        bullets = [];
        alienBullets = [];
        shields = [];
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

