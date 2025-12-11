// DOOM-style 3D FPS Game
window.doom = (function() {
    let canvas, ctx;
    let gameRunning = false;
    let gamePaused = false;
    let animationId;
    
    // Player
    let player = {
        x: 5,
        y: 5,
        angle: 0,
        health: 100,
        ammo: 50,
        score: 0,
        kills: 0
    };
    
    // Game state
    let enemies = [];
    let bullets = [];
    let pickups = [];
    let wallHits = [];
    let lastTime = 0;
    let lastSpawnTime = 0;
    
    // Map (1 = wall, 0 = empty)
    const mapWidth = 16;
    const mapHeight = 16;
    let map = [
        [1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1],
        [1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1],
        [1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1],
        [1,0,0,0,1,1,1,0,0,1,1,1,0,0,0,1],
        [1,0,0,0,0,0,1,0,0,1,0,0,0,0,0,1],
        [1,0,0,0,0,0,1,0,0,1,0,0,0,0,0,1],
        [1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1],
        [1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1],
        [1,0,0,1,1,0,0,0,0,0,0,1,1,0,0,1],
        [1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1],
        [1,0,0,0,0,0,1,0,0,1,0,0,0,0,0,1],
        [1,0,0,0,0,0,1,0,0,1,0,0,0,0,0,1],
        [1,0,0,0,1,1,1,0,0,1,1,1,0,0,0,1],
        [1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1],
        [1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1],
        [1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1]
    ];
    
    // Controls
    const keys = {};
    
    function init() {
        canvas = document.getElementById('doomCanvas');
        if (!canvas) {
            console.error('Canvas element not found');
            return;
        }
        ctx = canvas.getContext('2d');
        
        // Setup keyboard controls
        document.addEventListener('keydown', (e) => {
            keys[e.key.toLowerCase()] = true;
            if (e.key === ' ' && gameRunning && !gamePaused) {
                shoot();
                e.preventDefault();
            }
            if (e.key.toLowerCase() === 'p') {
                togglePause();
                e.preventDefault();
            }
        });
        
        document.addEventListener('keyup', (e) => {
            keys[e.key.toLowerCase()] = false;
        });
    }
    
    function startGame() {
        if (gameRunning) return;
        
        gameRunning = true;
        gamePaused = false;
        
        // Reset player
        player = {
            x: 5,
            y: 5,
            angle: 0,
            health: 150,
            ammo: 50,
            score: 0,
            kills: 0,
            maxHealth: 150
        };
        
        // Reset game state
        enemies = [];
        bullets = [];
        pickups = [];
        wallHits = [];
        enemyProjectiles = [];
        lastAttacker = null;
        lastSpawnTime = performance.now();
        
        // Spawn enemies
        spawnEnemies();
        
        // Spawn pickups
        spawnPickups();
        
        updateStats();
        lastTime = performance.now();
        animationId = requestAnimationFrame(gameLoop);
    }
    
    function stopGame() {
        gameRunning = false;
        gamePaused = false;
        if (animationId) {
            cancelAnimationFrame(animationId);
        }
        
        // Clear canvas
        ctx.fillStyle = '#000';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        
        // Show game over
        ctx.fillStyle = '#fff';
        ctx.font = '48px Arial';
        ctx.textAlign = 'center';
        ctx.fillText('GAME OVER', canvas.width / 2, canvas.height / 2);
        ctx.font = '24px Arial';
        ctx.fillText(`Final Score: ${player.score}`, canvas.width / 2, canvas.height / 2 + 40);
        ctx.fillText(`Kills: ${player.kills}`, canvas.width / 2, canvas.height / 2 + 70);
    }
    
    function togglePause() {
        if (!gameRunning) return;
        gamePaused = !gamePaused;
        if (!gamePaused) {
            lastTime = performance.now();
            animationId = requestAnimationFrame(gameLoop);
        }
    }
    
    function spawnEnemies() {
        const positions = [
            {x: 12, y: 3},
            {x: 3, y: 12}
        ];
        
        positions.forEach(pos => {
            enemies.push({
                x: pos.x + 0.5,
                y: pos.y + 0.5,
                health: 100,
                speed: 0.005,
                lastShot: 0,
                shootCooldown: 5000,
                damage: 3,
                attacking: false,
                attackTime: 0,
                state: 'idle', // idle, alert, chase, attack
                alertTime: 0,
                wanderAngle: Math.random() * Math.PI * 2,
                wanderChangeTime: 0,
                lastSeenPlayerX: null,
                lastSeenPlayerY: null,
                detectionRange: 7,
                attackRange: 3.5,
                loseInterestRange: 12
            });
        });
    }
    
    function spawnNewEnemy() {
        let attempts = 0;
        let spawned = false;
        
        // Difficulty scaling based on kills
        const difficultyMultiplier = 1 + (player.kills * 0.05);
        const enemySpeed = Math.min(0.008, 0.005 + (player.kills * 0.0002));
        const enemyHealth = Math.min(150, 100 + (player.kills * 2));
        const enemyDamage = Math.min(5, 3 + Math.floor(player.kills / 5));
        
        while (!spawned && attempts < 30) {
            const newX = 2 + Math.random() * 12;
            const newY = 2 + Math.random() * 12;
            const testMapX = Math.floor(newX);
            const testMapY = Math.floor(newY);
            
            // Make sure spawn location is far from player
            const distToPlayer = Math.sqrt((newX - player.x) ** 2 + (newY - player.y) ** 2);
            
            if (testMapX >= 0 && testMapX < mapWidth && testMapY >= 0 && testMapY < mapHeight && 
                map[testMapY][testMapX] === 0 && distToPlayer > 6) {
                enemies.push({
                    x: newX,
                    y: newY,
                    health: enemyHealth,
                    speed: enemySpeed + Math.random() * 0.002,
                    lastShot: performance.now() + 3000,
                    shootCooldown: Math.max(3500, 5000 - (player.kills * 50)),
                    damage: enemyDamage,
                    attacking: false,
                    attackTime: 0,
                    state: 'idle',
                    alertTime: 0,
                    wanderAngle: Math.random() * Math.PI * 2,
                    wanderChangeTime: performance.now(),
                    lastSeenPlayerX: null,
                    lastSeenPlayerY: null,
                    detectionRange: Math.min(9, 7 + Math.floor(player.kills / 5)),
                    attackRange: 3.5,
                    loseInterestRange: 12
                });
                spawned = true;
                console.log(`New demon spawned! Total: ${enemies.length}, Difficulty: ${difficultyMultiplier.toFixed(2)}x`);
            }
            attempts++;
        }
        
        return spawned;
    }
    
    function spawnPickups() {
        // Health pickups
        pickups.push({x: 2, y: 2, type: 'health'});
        pickups.push({x: 13, y: 13, type: 'health'});
        
        // Ammo pickups
        pickups.push({x: 13, y: 2, type: 'ammo'});
        pickups.push({x: 2, y: 13, type: 'ammo'});
        pickups.push({x: 8, y: 8, type: 'ammo'});
    }
    
    let lastShotTime = 0;
    const shootCooldown = 250; // milliseconds between shots
    
    function shoot() {
        const currentTime = performance.now();
        if (!gameRunning || player.ammo <= 0 || currentTime - lastShotTime < shootCooldown) return;
        
        // Validate player position
        if (isNaN(player.x) || isNaN(player.y) || isNaN(player.angle)) {
            console.error('Invalid player position or angle');
            return;
        }
        
        player.ammo--;
        lastShotTime = currentTime;
        updateStats();
        
        console.log('Shooting! Angle:', player.angle, 'Position:', player.x, player.y);
        
        // Create bullet starting slightly in front of player
        const spawnDist = 0.3;
        const bulletX = player.x + Math.cos(player.angle) * spawnDist;
        const bulletY = player.y + Math.sin(player.angle) * spawnDist;
        
        // Validate bullet position
        if (!isNaN(bulletX) && !isNaN(bulletY)) {
            bullets.push({
                x: bulletX,
                y: bulletY,
                angle: player.angle,
                speed: 0.01,
                damage: 50,
                lifetime: 0,
                maxLifetime: 3000
            });
        }
        
        // Muzzle flash effect
        muzzleFlashTime = currentTime;
    }
    
    let muzzleFlashTime = 0;
    
    function updatePlayer(deltaTime) {
        const moveSpeed = 0.002 * deltaTime;
        const rotSpeed = 0.003 * deltaTime;
        
        // Rotation
        if (keys['arrowleft']) {
            player.angle -= rotSpeed;
        }
        if (keys['arrowright']) {
            player.angle += rotSpeed;
        }
        
        // Movement
        let newX = player.x;
        let newY = player.y;
        
        if (keys['arrowup'] || keys['w']) {
            newX += Math.cos(player.angle) * moveSpeed;
            newY += Math.sin(player.angle) * moveSpeed;
        }
        if (keys['arrowdown'] || keys['s']) {
            newX -= Math.cos(player.angle) * moveSpeed;
            newY -= Math.sin(player.angle) * moveSpeed;
        }
        
        // Strafing
        if (keys['a']) {
            newX += Math.cos(player.angle - Math.PI / 2) * moveSpeed;
            newY += Math.sin(player.angle - Math.PI / 2) * moveSpeed;
        }
        if (keys['d']) {
            newX += Math.cos(player.angle + Math.PI / 2) * moveSpeed;
            newY += Math.sin(player.angle + Math.PI / 2) * moveSpeed;
        }
        
        // Collision detection with buffer
        const buffer = 0.2;
        const mapX = Math.floor(newX);
        const mapY = Math.floor(newY);
        
        // Check the cell and surrounding cells for walls
        let canMoveX = true;
        let canMoveY = true;
        
        if (mapX >= 0 && mapX < mapWidth && mapY >= 0 && mapY < mapHeight) {
            // Check X movement
            const testMapX = Math.floor(newX);
            const testMapYCurrent = Math.floor(player.y);
            if (map[testMapYCurrent][testMapX] !== 0) {
                canMoveX = false;
            }
            
            // Check Y movement
            const testMapXCurrent = Math.floor(player.x);
            const testMapY = Math.floor(newY);
            if (map[testMapY][testMapXCurrent] !== 0) {
                canMoveY = false;
            }
            
            // Check diagonal
            if (map[mapY][mapX] !== 0) {
                canMoveX = false;
                canMoveY = false;
            }
            
            if (canMoveX) {
                player.x = newX;
            }
            if (canMoveY) {
                player.y = newY;
            }
        }
        
        // Check pickup collision
        for (let i = pickups.length - 1; i >= 0; i--) {
            const pickup = pickups[i];
            const dx = player.x - pickup.x;
            const dy = player.y - pickup.y;
            const dist = Math.sqrt(dx * dx + dy * dy);
            
            if (dist < 0.5) {
                if (pickup.type === 'health') {
                    player.health = Math.min(player.maxHealth, player.health + 30);
                } else if (pickup.type === 'ammo') {
                    player.ammo += 20;
                }
                pickups.splice(i, 1);
                updateStats();
            }
        }
    }
    
    function updateEnemies(deltaTime, currentTime) {
        for (let i = enemies.length - 1; i >= 0; i--) {
            const enemy = enemies[i];
            
            // Calculate distance to player
            const dx = player.x - enemy.x;
            const dy = player.y - enemy.y;
            const dist = Math.sqrt(dx * dx + dy * dy);
            
            // Check line of sight to player
            const hasLineOfSight = checkLineOfSight(enemy.x, enemy.y, player.x, player.y);
            
            // State machine
            switch(enemy.state) {
                case 'idle':
                    // Wander around randomly
                    if (currentTime - enemy.wanderChangeTime > 2000) {
                        enemy.wanderAngle = Math.random() * Math.PI * 2;
                        enemy.wanderChangeTime = currentTime;
                    }
                    
                    // Move in wander direction
                    const wanderSpeed = enemy.speed * 0.5 * deltaTime;
                    const wanderX = enemy.x + Math.cos(enemy.wanderAngle) * wanderSpeed;
                    const wanderY = enemy.y + Math.sin(enemy.wanderAngle) * wanderSpeed;
                    
                    if (canMoveTo(enemy, wanderX, wanderY)) {
                        enemy.x = wanderX;
                        enemy.y = wanderY;
                    } else {
                        // Hit a wall, change direction
                        enemy.wanderAngle = Math.random() * Math.PI * 2;
                    }
                    
                    // Check if player is in detection range
                    if (dist < enemy.detectionRange && hasLineOfSight) {
                        enemy.state = 'alert';
                        enemy.alertTime = currentTime;
                    }
                    break;
                    
                case 'alert':
                    // Stop and look around for a moment
                    if (currentTime - enemy.alertTime > 500) {
                        if (dist < enemy.detectionRange && hasLineOfSight) {
                            enemy.state = 'chase';
                            enemy.lastSeenPlayerX = player.x;
                            enemy.lastSeenPlayerY = player.y;
                        } else {
                            enemy.state = 'idle';
                        }
                    }
                    break;
                    
                case 'chase':
                    // Update last seen position if we can see player
                    if (hasLineOfSight && dist < enemy.loseInterestRange) {
                        enemy.lastSeenPlayerX = player.x;
                        enemy.lastSeenPlayerY = player.y;
                    }
                    
                    // Chase the player (or last seen position)
                    let targetX = enemy.lastSeenPlayerX || player.x;
                    let targetY = enemy.lastSeenPlayerY || player.y;
                    
                    const chaseDx = targetX - enemy.x;
                    const chaseDy = targetY - enemy.y;
                    const chaseDist = Math.sqrt(chaseDx * chaseDx + chaseDy * chaseDy);
                    
                    // Keep some distance, don't get too close
                    if (chaseDist > 2.5) {
                        const chaseSpeed = enemy.speed * deltaTime;
                        const moveX = (chaseDx / chaseDist) * chaseSpeed;
                        const moveY = (chaseDy / chaseDist) * chaseSpeed;
                        
                        const newX = enemy.x + moveX;
                        const newY = enemy.y + moveY;
                        
                        if (canMoveTo(enemy, newX, enemy.y)) {
                            enemy.x = newX;
                        }
                        if (canMoveTo(enemy, enemy.x, newY)) {
                            enemy.y = newY;
                        }
                    }
                    
                    // Enter attack range
                    if (dist < enemy.attackRange && hasLineOfSight) {
                        enemy.state = 'attack';
                    }
                    
                    // Lose interest if player is too far
                    if (dist > enemy.loseInterestRange || (chaseDist < 0.5 && !hasLineOfSight)) {
                        enemy.state = 'idle';
                        enemy.lastSeenPlayerX = null;
                        enemy.lastSeenPlayerY = null;
                    }
                    break;
                    
                case 'attack':
                    // Stop moving and attack
                    enemy.attacking = false;
                    
                    // Show warning when about to attack (1.5 seconds before)
                    if (currentTime - enemy.lastShot > enemy.shootCooldown - 1500) {
                        enemy.attacking = true;
                    }
                    
                    // Fire!
                    if (currentTime - enemy.lastShot > enemy.shootCooldown) {
                        player.health -= enemy.damage;
                        enemy.lastShot = currentTime;
                        enemy.attackTime = currentTime;
                        enemy.attacking = false;
                        updateStats();
                        
                        // Flash red on hit with attacker info
                        flashDamage(enemy);
                        
                        // Create attack projectile visual
                        createEnemyProjectile(enemy.x, enemy.y, player.x, player.y);
                        
                        if (player.health <= 0) {
                            stopGame();
                            return;
                        }
                        
                        // After attacking, go back to chase
                        enemy.state = 'chase';
                    }
                    
                    // If player moves out of range, chase again
                    if (dist > enemy.attackRange) {
                        enemy.state = 'chase';
                        enemy.attacking = false;
                    }
                    
                    // Lose line of sight
                    if (!hasLineOfSight) {
                        enemy.state = 'chase';
                        enemy.lastSeenPlayerX = player.x;
                        enemy.lastSeenPlayerY = player.y;
                        enemy.attacking = false;
                    }
                    break;
            }
        }
    }
    
    // Helper function to check if enemy can move to a position
    function canMoveTo(enemy, newX, newY) {
        const buffer = 0.3;
        const corners = [
            {x: newX - buffer, y: newY - buffer},
            {x: newX + buffer, y: newY - buffer},
            {x: newX - buffer, y: newY + buffer},
            {x: newX + buffer, y: newY + buffer}
        ];
        
        for (const corner of corners) {
            const cx = Math.floor(corner.x);
            const cy = Math.floor(corner.y);
            if (cx < 0 || cx >= mapWidth || cy < 0 || cy >= mapHeight || map[cy][cx] !== 0) {
                return false;
            }
        }
        return true;
    }
    
    // Helper function to check line of sight between two points
    function checkLineOfSight(x1, y1, x2, y2) {
        // Validate input
        if (typeof x1 !== 'number' || typeof y1 !== 'number' || 
            typeof x2 !== 'number' || typeof y2 !== 'number' ||
            isNaN(x1) || isNaN(y1) || isNaN(x2) || isNaN(y2)) {
            return false;
        }
        
        const dx = x2 - x1;
        const dy = y2 - y1;
        const dist = Math.sqrt(dx * dx + dy * dy);
        
        // Handle zero distance
        if (dist < 0.01) {
            return true;
        }
        
        const steps = Math.max(1, Math.floor(dist * 2));
        
        for (let i = 0; i <= steps; i++) {
            const t = i / steps;
            const checkX = x1 + dx * t;
            const checkY = y1 + dy * t;
            const mapX = Math.floor(checkX);
            const mapY = Math.floor(checkY);
            
            // Validate array bounds
            if (mapX < 0 || mapX >= mapWidth || mapY < 0 || mapY >= mapHeight) {
                return false;
            }
            
            // Check if position is valid in map
            if (!map[mapY] || map[mapY][mapX] === undefined || map[mapY][mapX] !== 0) {
                return false;
            }
        }
        return true;
    }
    
    // Enemy projectiles for visual effect
    let enemyProjectiles = [];
    
    function createEnemyProjectile(fromX, fromY, toX, toY) {
        const angle = Math.atan2(toY - fromY, toX - fromX);
        enemyProjectiles.push({
            x: fromX,
            y: fromY,
            angle: angle,
            speed: 0.015,
            life: 0,
            maxLife: 500
        });
    }
    
    function updateEnemyProjectiles(deltaTime) {
        for (let i = enemyProjectiles.length - 1; i >= 0; i--) {
            const proj = enemyProjectiles[i];
            
            proj.life += deltaTime;
            if (proj.life > proj.maxLife) {
                enemyProjectiles.splice(i, 1);
                continue;
            }
            
            proj.x += Math.cos(proj.angle) * proj.speed * deltaTime;
            proj.y += Math.sin(proj.angle) * proj.speed * deltaTime;
        }
    }
    
    let damageFlashTime = 0;
    let lastAttacker = null;
    
    function flashDamage(attacker) {
        damageFlashTime = performance.now();
        lastAttacker = attacker ? {x: attacker.x, y: attacker.y} : null;
    }
    
    function updateBullets(deltaTime) {
        for (let i = bullets.length - 1; i >= 0; i--) {
            const bullet = bullets[i];
            
            // Update lifetime
            bullet.lifetime += deltaTime;
            if (bullet.lifetime > bullet.maxLifetime) {
                bullets.splice(i, 1);
                continue;
            }
            
            // Move bullet
            bullet.x += Math.cos(bullet.angle) * bullet.speed * deltaTime;
            bullet.y += Math.sin(bullet.angle) * bullet.speed * deltaTime;
            
            let bulletRemoved = false;
            
            // Check enemy collision FIRST (before wall check)
            for (let j = enemies.length - 1; j >= 0; j--) {
                const enemy = enemies[j];
                const dx = bullet.x - enemy.x;
                const dy = bullet.y - enemy.y;
                const dist = Math.sqrt(dx * dx + dy * dy);
                
                // Larger hitbox for better hit detection
                if (dist < 0.5) {
                    enemy.health -= bullet.damage;
                    console.log(`Hit enemy! Health: ${enemy.health}`);
                    bullets.splice(i, 1);
                    bulletRemoved = true;
                    
                    if (enemy.health <= 0) {
                        console.log('Enemy killed!');
                        enemies.splice(j, 1);
                        player.kills++;
                        player.score += 100;
                        updateStats();
                        
                        // Dynamic max enemies based on kills (increases difficulty)
                        const maxEnemies = Math.min(8, 3 + Math.floor(player.kills / 3));
                        
                        // Spawn new enemy after a delay
                        if (enemies.length < maxEnemies) {
                            const spawnDelay = Math.max(2000, 4000 - (player.kills * 100)); // Faster spawning as game progresses
                            
                            setTimeout(() => {
                                if (gameRunning && enemies.length < maxEnemies) {
                                    spawnNewEnemy();
                                }
                            }, spawnDelay);
                        }
                    }
                    break;
                }
            }
            
            if (bulletRemoved) continue;
            
            // Check wall collision
            const mapX = Math.floor(bullet.x);
            const mapY = Math.floor(bullet.y);
            
            if (mapX < 0 || mapX >= mapWidth || mapY < 0 || mapY >= mapHeight || map[mapY][mapX] === 1) {
                wallHits.push({x: bullet.x, y: bullet.y, time: performance.now()});
                bullets.splice(i, 1);
            }
        }
        
        // Clean old wall hits
        wallHits = wallHits.filter(hit => performance.now() - hit.time < 100);
    }
    
    function render() {
        const screenWidth = canvas.width;
        const screenHeight = canvas.height;
        const currentTime = performance.now();
        
        // Draw floor and ceiling
        ctx.fillStyle = '#333';
        ctx.fillRect(0, 0, screenWidth, screenHeight / 2);
        ctx.fillStyle = '#222';
        ctx.fillRect(0, screenHeight / 2, screenWidth, screenHeight / 2);
        
        // Raycasting
        const fov = Math.PI / 3;
        const numRays = screenWidth;
        const maxDepth = 20;
        
        const sprites = [];
        
        for (let ray = 0; ray < numRays; ray++) {
            const rayAngle = player.angle - fov / 2 + (ray / numRays) * fov;
            
            let distanceToWall = 0;
            let hitWall = false;
            let wallType = 0;
            
            const eyeX = Math.cos(rayAngle);
            const eyeY = Math.sin(rayAngle);
            
            while (!hitWall && distanceToWall < maxDepth) {
                distanceToWall += 0.1;
                
                const testX = player.x + eyeX * distanceToWall;
                const testY = player.y + eyeY * distanceToWall;
                
                const mapX = Math.floor(testX);
                const mapY = Math.floor(testY);
                
                if (mapX < 0 || mapX >= mapWidth || mapY < 0 || mapY >= mapHeight) {
                    hitWall = true;
                    distanceToWall = maxDepth;
                } else if (map[mapY][mapX] === 1) {
                    hitWall = true;
                }
            }
            
            // Calculate wall height
            const correctedDistance = distanceToWall * Math.cos(rayAngle - player.angle);
            const wallHeight = (screenHeight / correctedDistance) * 0.5;
            
            // Draw wall slice
            const ceiling = (screenHeight / 2) - wallHeight;
            const floor = (screenHeight / 2) + wallHeight;
            
            // Wall shading based on distance
            const brightness = Math.max(0, 255 - (correctedDistance * 20));
            ctx.fillStyle = `rgb(${brightness}, ${brightness * 0.5}, ${brightness * 0.5})`;
            ctx.fillRect(ray, ceiling, 1, floor - ceiling);
            
            // Add wall edge darkening
            if (distanceToWall < maxDepth) {
                const wallEdgeDarkness = Math.abs(Math.sin(distanceToWall * 5)) * 20;
                ctx.fillStyle = `rgba(0, 0, 0, ${wallEdgeDarkness / 255})`;
                ctx.fillRect(ray, ceiling, 1, floor - ceiling);
            }
        }
        
        // Collect all sprites with their properties
        const allSprites = [
            ...enemies.map(e => ({...e, type: 'enemy'})),
            ...pickups,
            ...bullets.map(b => ({x: b.x, y: b.y, type: 'bullet'})),
            ...enemyProjectiles.map(p => ({x: p.x, y: p.y, type: 'enemy_projectile'}))
        ];
        
        // Draw enemies, pickups, and bullets as sprites
        allSprites.forEach(obj => {
            // Validate object has coordinates
            if (typeof obj.x !== 'number' || typeof obj.y !== 'number' || 
                isNaN(obj.x) || isNaN(obj.y)) {
                return;
            }
            
            const dx = obj.x - player.x;
            const dy = obj.y - player.y;
            const dist = Math.sqrt(dx * dx + dy * dy);
            const angle = Math.atan2(dy, dx) - player.angle;
            
            // Check if object is in front of player
            if (Math.cos(angle) > 0) {
                // Check line of sight - only render if visible (not behind walls)
                const hasLineOfSight = checkLineOfSight(player.x, player.y, obj.x, obj.y);
                
                if (hasLineOfSight) {
                    sprites.push({
                        dist: dist,
                        angle: angle,
                        type: obj.type || 'enemy',
                        health: obj.health,
                        x: obj.x,
                        y: obj.y
                    });
                }
            }
        });
        
        // Sort sprites by distance (far to near)
        sprites.sort((a, b) => b.dist - a.dist);
        
        // Draw sprites
        sprites.forEach(sprite => {
            const correctedDist = sprite.dist * Math.cos(sprite.angle);
            const spriteHeight = (screenHeight / correctedDist) * 0.5;
            const spriteWidth = spriteHeight;
            
            const spriteX = (screenWidth / 2) + (Math.tan(sprite.angle) * screenWidth);
            
            if (spriteX > -spriteWidth && spriteX < screenWidth + spriteWidth) {
                const brightness = Math.max(0, 255 - (correctedDist * 20));
                
                if (sprite.type === 'enemy') {
                    // Find the actual enemy object to check attack state
                    const enemyObj = enemies.find(e => 
                        sprite.x !== undefined && sprite.y !== undefined &&
                        Math.abs(e.x - sprite.x) < 0.01 && Math.abs(e.y - sprite.y) < 0.01
                    );
                    const isAttacking = enemyObj && enemyObj.attacking;
                    const justAttacked = enemyObj && (currentTime - enemyObj.attackTime < 200);
                    const enemyState = enemyObj ? enemyObj.state : 'idle';
                    
                    // Draw glow when attacking
                    if (isAttacking) {
                        ctx.fillStyle = `rgba(255, 100, 0, ${0.3 * Math.sin(currentTime / 100)})`;
                        ctx.fillRect(spriteX - spriteWidth * 0.6, screenHeight / 2 - spriteHeight * 0.6, spriteWidth * 1.2, spriteHeight * 1.2);
                    }
                    
                    // Draw alert glow
                    if (enemyState === 'alert') {
                        ctx.fillStyle = `rgba(255, 255, 0, ${0.2 * Math.sin(currentTime / 150)})`;
                        ctx.fillRect(spriteX - spriteWidth * 0.6, screenHeight / 2 - spriteHeight * 0.6, spriteWidth * 1.2, spriteHeight * 1.2);
                    }
                    
                    // Draw enemy body with color based on state
                    let bodyColor;
                    switch(enemyState) {
                        case 'idle':
                            bodyColor = `rgb(${brightness * 0.3}, ${brightness * 0.6}, ${brightness * 0.3})`; // Green
                            break;
                        case 'alert':
                            bodyColor = `rgb(${brightness * 0.6}, ${brightness * 0.6}, ${brightness * 0.3})`; // Yellow-green
                            break;
                        case 'chase':
                            bodyColor = `rgb(${brightness * 0.6}, ${brightness * 0.5}, ${brightness * 0.3})`; // Orange-green
                            break;
                        case 'attack':
                            bodyColor = `rgb(${brightness * 0.8}, ${brightness * 0.3}, ${brightness * 0.3})`; // Red
                            break;
                        default:
                            bodyColor = `rgb(${brightness * 0.3}, ${brightness * 0.8}, ${brightness * 0.3})`;
                    }
                    ctx.fillStyle = bodyColor;
                    ctx.fillRect(spriteX - spriteWidth / 2, screenHeight / 2 - spriteHeight / 2, spriteWidth, spriteHeight);
                    
                    // Draw muzzle flash when just attacked
                    if (justAttacked) {
                        const flashSize = spriteWidth * 0.5;
                        const flashAlpha = 1 - ((currentTime - enemyObj.attackTime) / 200);
                        ctx.fillStyle = `rgba(255, 200, 0, ${flashAlpha})`;
                        ctx.beginPath();
                        ctx.arc(spriteX, screenHeight / 2, flashSize, 0, Math.PI * 2);
                        ctx.fill();
                    }
                    
                    // Draw eyes (glow red when attacking)
                    if (isAttacking) {
                        ctx.fillStyle = `rgb(255, ${100 * Math.sin(currentTime / 100)}, 0)`;
                        ctx.shadowColor = 'red';
                        ctx.shadowBlur = 10;
                    } else {
                        ctx.fillStyle = 'red';
                        ctx.shadowBlur = 0;
                    }
                    const eyeSize = spriteWidth / 6;
                    ctx.fillRect(spriteX - spriteWidth / 4, screenHeight / 2 - spriteHeight / 4, eyeSize, eyeSize);
                    ctx.fillRect(spriteX + spriteWidth / 12, screenHeight / 2 - spriteHeight / 4, eyeSize, eyeSize);
                    ctx.shadowBlur = 0;
                    
                    // Draw health bar above enemy
                    if (sprite.health !== undefined && sprite.health < 100) {
                        const barWidth = spriteWidth * 0.8;
                        const barHeight = 5;
                        const barY = screenHeight / 2 - spriteHeight / 2 - 10;
                        
                        // Background
                        ctx.fillStyle = 'rgba(100, 0, 0, 0.8)';
                        ctx.fillRect(spriteX - barWidth / 2, barY, barWidth, barHeight);
                        
                        // Health
                        ctx.fillStyle = 'rgba(0, 255, 0, 0.8)';
                        ctx.fillRect(spriteX - barWidth / 2, barY, barWidth * (sprite.health / 100), barHeight);
                    }
                    
                    // Draw warning indicator when attacking
                    if (isAttacking) {
                        ctx.fillStyle = 'rgba(255, 0, 0, 0.8)';
                        ctx.font = 'bold 20px Arial';
                        ctx.textAlign = 'center';
                        ctx.fillText('!', spriteX, screenHeight / 2 - spriteHeight / 2 - 20);
                    }
                } else if (sprite.type === 'health') {
                    // Draw health pickup
                    ctx.fillStyle = `rgb(${brightness}, ${brightness * 0.3}, ${brightness * 0.3})`;
                    ctx.fillRect(spriteX - spriteWidth / 4, screenHeight / 2 - spriteHeight / 4, spriteWidth / 2, spriteHeight / 2);
                    ctx.fillRect(spriteX - spriteWidth / 8, screenHeight / 2 - spriteHeight / 2, spriteWidth / 4, spriteHeight);
                } else if (sprite.type === 'ammo') {
                    // Draw ammo pickup
                    ctx.fillStyle = `rgb(${brightness * 0.8}, ${brightness * 0.8}, ${brightness * 0.3})`;
                    ctx.fillRect(spriteX - spriteWidth / 4, screenHeight / 2 - spriteHeight / 4, spriteWidth / 2, spriteHeight / 2);
                } else if (sprite.type === 'bullet') {
                    // Draw bullet as yellow projectile
                    ctx.fillStyle = `rgb(${brightness}, ${brightness}, 0)`;
                    const bulletSize = Math.max(3, spriteWidth / 4);
                    ctx.fillRect(spriteX - bulletSize / 2, screenHeight / 2 - bulletSize / 2, bulletSize, bulletSize);
                } else if (sprite.type === 'enemy_projectile') {
                    // Draw enemy projectile as red plasma ball
                    const projSize = Math.max(4, spriteWidth / 3);
                    
                    // Outer glow
                    ctx.fillStyle = `rgba(255, 0, 0, ${0.3 * brightness / 255})`;
                    ctx.beginPath();
                    ctx.arc(spriteX, screenHeight / 2, projSize * 1.5, 0, Math.PI * 2);
                    ctx.fill();
                    
                    // Inner core
                    ctx.fillStyle = `rgb(${brightness}, ${brightness * 0.2}, 0)`;
                    ctx.beginPath();
                    ctx.arc(spriteX, screenHeight / 2, projSize, 0, Math.PI * 2);
                    ctx.fill();
                }
            }
        });
        
        // Draw HUD
        drawHUD();
        
        // Draw damage flash with directional indicator
        if (currentTime - damageFlashTime < 200) {
            const alpha = 0.3 * (1 - (currentTime - damageFlashTime) / 200);
            ctx.fillStyle = `rgba(255, 0, 0, ${alpha})`;
            ctx.fillRect(0, 0, screenWidth, screenHeight);
            
            // Draw attack direction indicators on screen edges
            if (lastAttacker) {
                const dx = lastAttacker.x - player.x;
                const dy = lastAttacker.y - player.y;
                const angle = Math.atan2(dy, dx) - player.angle;
                
                // Normalize angle
                let normalizedAngle = angle;
                while (normalizedAngle < -Math.PI) normalizedAngle += Math.PI * 2;
                while (normalizedAngle > Math.PI) normalizedAngle -= Math.PI * 2;
                
                // Draw arrow on screen edge
                const arrowSize = 30;
                const edgeMargin = 50;
                
                if (normalizedAngle > -Math.PI/4 && normalizedAngle < Math.PI/4) {
                    // Right
                    ctx.fillStyle = `rgba(255, 0, 0, ${alpha * 2})`;
                    ctx.beginPath();
                    ctx.moveTo(screenWidth - edgeMargin, screenHeight / 2);
                    ctx.lineTo(screenWidth - edgeMargin - arrowSize, screenHeight / 2 - arrowSize / 2);
                    ctx.lineTo(screenWidth - edgeMargin - arrowSize, screenHeight / 2 + arrowSize / 2);
                    ctx.fill();
                } else if (normalizedAngle > Math.PI/4 && normalizedAngle < 3*Math.PI/4) {
                    // Bottom
                    ctx.fillStyle = `rgba(255, 0, 0, ${alpha * 2})`;
                    ctx.beginPath();
                    ctx.moveTo(screenWidth / 2, screenHeight - edgeMargin);
                    ctx.lineTo(screenWidth / 2 - arrowSize / 2, screenHeight - edgeMargin - arrowSize);
                    ctx.lineTo(screenWidth / 2 + arrowSize / 2, screenHeight - edgeMargin - arrowSize);
                    ctx.fill();
                } else if (normalizedAngle < -Math.PI/4 && normalizedAngle > -3*Math.PI/4) {
                    // Top
                    ctx.fillStyle = `rgba(255, 0, 0, ${alpha * 2})`;
                    ctx.beginPath();
                    ctx.moveTo(screenWidth / 2, edgeMargin);
                    ctx.lineTo(screenWidth / 2 - arrowSize / 2, edgeMargin + arrowSize);
                    ctx.lineTo(screenWidth / 2 + arrowSize / 2, edgeMargin + arrowSize);
                    ctx.fill();
                } else {
                    // Left
                    ctx.fillStyle = `rgba(255, 0, 0, ${alpha * 2})`;
                    ctx.beginPath();
                    ctx.moveTo(edgeMargin, screenHeight / 2);
                    ctx.lineTo(edgeMargin + arrowSize, screenHeight / 2 - arrowSize / 2);
                    ctx.lineTo(edgeMargin + arrowSize, screenHeight / 2 + arrowSize / 2);
                    ctx.fill();
                }
            }
        }
        
        // Draw muzzle flash
        if (currentTime - muzzleFlashTime < 100) {
            const flashAlpha = 0.5 * (1 - (currentTime - muzzleFlashTime) / 100);
            ctx.fillStyle = `rgba(255, 255, 0, ${flashAlpha})`;
            ctx.beginPath();
            ctx.arc(screenWidth / 2, screenHeight / 2, 30, 0, Math.PI * 2);
            ctx.fill();
        }
        
        // Draw crosshair
        const crosshairColor = player.ammo > 0 ? '#0f0' : '#f00';
        ctx.strokeStyle = crosshairColor;
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(screenWidth / 2 - 10, screenHeight / 2);
        ctx.lineTo(screenWidth / 2 + 10, screenHeight / 2);
        ctx.moveTo(screenWidth / 2, screenHeight / 2 - 10);
        ctx.lineTo(screenWidth / 2, screenHeight / 2 + 10);
        ctx.stroke();
        
        // Draw pause overlay
        if (gamePaused) {
            ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
            ctx.fillRect(0, 0, screenWidth, screenHeight);
            ctx.fillStyle = '#fff';
            ctx.font = '48px Arial';
            ctx.textAlign = 'center';
            ctx.fillText('PAUSED', screenWidth / 2, screenHeight / 2);
            ctx.font = '24px Arial';
            ctx.fillText('Press P to resume', screenWidth / 2, screenHeight / 2 + 40);
        }
    }
    
    function drawHUD() {
        const padding = 10;
        
        // Health bar background
        ctx.fillStyle = '#400';
        ctx.fillRect(padding, canvas.height - 45, 220, 35);
        
        // Health bar fill
        let healthColor = '#0f0';
        const healthPercent = (player.health / player.maxHealth) * 100;
        if (healthPercent < 25) {
            healthColor = '#f00';
        } else if (healthPercent < 50) {
            healthColor = '#ff0';
        }
        ctx.fillStyle = healthColor;
        ctx.fillRect(padding + 3, canvas.height - 42, (player.health / player.maxHealth) * 214, 29);
        
        // Health bar border
        ctx.strokeStyle = '#fff';
        ctx.lineWidth = 2;
        ctx.strokeRect(padding, canvas.height - 45, 220, 35);
        
        // Health text
        ctx.fillStyle = '#fff';
        ctx.font = 'bold 18px Arial';
        ctx.textAlign = 'left';
        ctx.shadowColor = '#000';
        ctx.shadowBlur = 4;
        ctx.fillText(`HEALTH: ${Math.max(0, player.health)}`, padding + 8, canvas.height - 20);
        ctx.shadowBlur = 0;
        
        // Low health warning
        if ((player.health / player.maxHealth) < 0.25) {
            const flash = Math.sin(performance.now() / 200) > 0;
            if (flash) {
                ctx.fillStyle = '#f00';
                ctx.font = 'bold 24px Arial';
                ctx.textAlign = 'center';
                ctx.shadowBlur = 6;
                ctx.fillText('LOW HEALTH!', canvas.width / 2, 40);
                ctx.shadowBlur = 0;
            }
        }
        
        // Ammo
        ctx.fillStyle = player.ammo > 10 ? '#fff' : '#f00';
        ctx.font = 'bold 24px Arial';
        ctx.textAlign = 'right';
        ctx.shadowColor = '#000';
        ctx.shadowBlur = 4;
        ctx.fillText(`AMMO: ${player.ammo}`, canvas.width - padding, canvas.height - 20);
        ctx.shadowBlur = 0;
        
        // Score
        ctx.fillStyle = '#fff';
        ctx.textAlign = 'right';
        ctx.shadowBlur = 4;
        ctx.fillText(`SCORE: ${player.score}`, canvas.width - padding, padding + 25);
        ctx.fillText(`KILLS: ${player.kills}`, canvas.width - padding, padding + 55);
        ctx.shadowBlur = 0;
        
        // Enemy count and wave info
        ctx.fillStyle = '#ff0';
        ctx.font = 'bold 20px Arial';
        ctx.textAlign = 'left';
        ctx.shadowBlur = 4;
        ctx.fillText(`DEMONS: ${enemies.length}`, padding, padding + 25);
        
        // Wave/Difficulty indicator
        const wave = 1 + Math.floor(player.kills / 3);
        ctx.fillStyle = '#f80';
        ctx.font = 'bold 18px Arial';
        ctx.fillText(`WAVE: ${wave}`, padding, padding + 50);
        ctx.shadowBlur = 0;
    }
    
    function updateStats() {
        // Update desktop stats
        updateElement('doom-health', player.health);
        updateElement('doom-ammo', player.ammo);
        updateElement('doom-kills', player.kills);
        updateElement('doom-score', player.score);
        
        // Update mobile stats
        updateElement('doom-health-mobile', player.health);
        updateElement('doom-ammo-mobile', player.ammo);
        updateElement('doom-kills-mobile', player.kills);
        updateElement('doom-score-mobile', player.score);
    }
    
    function updateElement(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = value;
        }
    }
    
    function gameLoop(currentTime) {
        if (!gameRunning || gamePaused) return;
        
        // Calculate delta time (capped to avoid huge jumps)
        const deltaTime = Math.min(currentTime - lastTime, 100);
        lastTime = currentTime;
        
        // Update game state - these should run every frame
        updatePlayer(deltaTime);
        updateEnemies(deltaTime, currentTime);
        updateBullets(deltaTime);
        updateEnemyProjectiles(deltaTime);
        
        // Periodic enemy spawning (every 15-30 seconds, faster as game progresses)
        const maxEnemies = Math.min(8, 3 + Math.floor(player.kills / 3));
        const spawnInterval = Math.max(15000, 30000 - (player.kills * 500)); // Faster spawning over time
        
        if (currentTime - lastSpawnTime > spawnInterval && enemies.length < maxEnemies) {
            if (spawnNewEnemy()) {
                lastSpawnTime = currentTime;
            }
        }
        // Render
        render();
        
        // Continue loop
        animationId = requestAnimationFrame(gameLoop);
    }
    
    // Initialize on load
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
    
    return {
        startGame,
        stopGame,
        togglePause
    };
})();

