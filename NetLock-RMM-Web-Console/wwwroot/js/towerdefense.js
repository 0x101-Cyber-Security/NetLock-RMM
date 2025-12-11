// Computer Virus Defense Game
(function() {
    let canvas;
    let ctx;
    let gameRunning = false;
    let gamePaused = false;
    let animationId;
    
    // Game state
    let gold = 200;
    let lives = 20;
    let wave = 0;
    let score = 0;
    let enemiesKilled = 0;
    
    // Grid settings
    const GRID_SIZE = 40;
    const COLS = 20;
    const ROWS = 15;
    
    // Game objects
    let towers = [];
    let enemies = [];
    let projectiles = [];
    let particles = [];
    
    // Selected tower type for placement
    let selectedTowerType = null;
    let hoveredCell = { x: -1, y: -1 };
    let demolishMode = false;
    
    // Path (Simplified network path)
    const path = [
        { x: 0, y: 7 },
        { x: 5, y: 7 },
        { x: 5, y: 3 },
        { x: 10, y: 3 },
        { x: 10, y: 10 },
        { x: 15, y: 10 },
        { x: 15, y: 5 },
        { x: 20, y: 5 }
    ];
    
    // Security Tool types (towers)
    const towerTypes = {
        basic: {
            name: 'Firewall',
            cost: 50,
            damage: 12,
            range: 120,
            fireRate: 1000,
            color: '#1565C0',
            projectileColor: '#64B5F6',
            description: 'Blocks incoming threats'
        },
        sniper: {
            name: 'Antivirus',
            cost: 100,
            damage: 45,
            range: 200,
            fireRate: 2000,
            color: '#2E7D32',
            projectileColor: '#81C784',
            description: 'Detects and removes viruses'
        },
        cannon: {
            name: 'Quarantine',
            cost: 150,
            damage: 30,
            range: 100,
            fireRate: 1500,
            color: '#6A1B9A',
            projectileColor: '#BA68C8',
            splash: true,
            splashRadius: 40,
            description: 'Isolates multiple threats'
        },
        rapid: {
            name: 'IDS',
            cost: 75,
            damage: 6,
            range: 100,
            fireRate: 300,
            color: '#FF8F00',
            projectileColor: '#FFD54F',
            description: 'Intrusion Detection System'
        }
    };
    
    // Virus types (Enemies)
    const enemyTypes = {
        basic: {
            health: 50,
            speed: 1,
            reward: 10,
            color: '#E53935',
            name: 'Worm',
            description: 'Simple network worm'
        },
        fast: {
            health: 30,
            speed: 2,
            reward: 15,
            color: '#8E24AA',
            name: 'Trojan',
            description: 'Fast trojan'
        },
        tank: {
            health: 150,
            speed: 0.5,
            reward: 30,
            color: '#3949AB',
            name: 'Rootkit',
            description: 'Persistent rootkit'
        },
        boss: {
            health: 500,
            speed: 0.3,
            reward: 100,
            color: '#FF7043',
            name: 'Ransomware',
            description: 'Dangerous ransomware'
        }
    };
    
    class Tower {
        constructor(gridX, gridY, type) {
            this.gridX = gridX;
            this.gridY = gridY;
            this.x = gridX * GRID_SIZE + GRID_SIZE / 2;
            this.y = gridY * GRID_SIZE + GRID_SIZE / 2;
            this.type = type;
            this.lastFireTime = 0;
            this.target = null;
            this.level = 1;
            this.maxLevel = 5;
        }
        
        getUpgradeCost() {
            return Math.floor(towerTypes[this.type].cost * 0.5 * this.level);
        }
        
        canUpgrade() {
            return this.level < this.maxLevel;
        }
        
        getRefundValue() {
            // Refund 70% of total invested (base cost + all upgrades)
            let totalCost = towerTypes[this.type].cost;
            for (let i = 1; i < this.level; i++) {
                totalCost += Math.floor(towerTypes[this.type].cost * 0.5 * i);
            }
            return Math.floor(totalCost * 0.7);
        }
        
        update(timestamp) {
            // Find target
            if (!this.target || this.target.dead || !this.isInRange(this.target)) {
                this.findTarget();
            }
            
            // Fire at target
            if (this.target && timestamp - this.lastFireTime >= towerTypes[this.type].fireRate) {
                this.fire();
                this.lastFireTime = timestamp;
            }
        }
        
        findTarget() {
            this.target = null;
            let closestDistance = Infinity;
            
            for (let enemy of enemies) {
                if (enemy.dead) continue;
                
                const distance = this.getDistance(enemy);
                if (distance <= towerTypes[this.type].range && distance < closestDistance) {
                    this.target = enemy;
                    closestDistance = distance;
                }
            }
        }
        
        isInRange(enemy) {
            return this.getDistance(enemy) <= towerTypes[this.type].range;
        }
        
        getDistance(enemy) {
            const dx = this.x - enemy.x;
            const dy = this.y - enemy.y;
            return Math.sqrt(dx * dx + dy * dy);
        }
        
        fire() {
            if (!this.target) return;
            
            projectiles.push(new Projectile(
                this.x, 
                this.y, 
                this.target, 
                towerTypes[this.type].damage * this.level,
                towerTypes[this.type].projectileColor,
                towerTypes[this.type].splash,
                towerTypes[this.type].splashRadius
            ));
        }
        
        draw() {
            // Draw range (when hovered)
            if (hoveredCell.x === this.gridX && hoveredCell.y === this.gridY) {
                ctx.beginPath();
                ctx.arc(this.x, this.y, towerTypes[this.type].range, 0, Math.PI * 2);
                ctx.fillStyle = 'rgba(255, 255, 255, 0.06)';
                ctx.fill();
                ctx.strokeStyle = 'rgba(255, 255, 255, 0.2)';
                ctx.lineWidth = 2;
                ctx.stroke();
                
                // Draw info when hovered
                ctx.fillStyle = 'rgba(0, 0, 0, 0.85)';
                ctx.fillRect(this.x - 60, this.y - 75, 120, 55);
                
                ctx.fillStyle = '#FFF';
                ctx.font = 'bold 13px Arial';
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                
                if (demolishMode) {
                    // Show demolish info
                    const refund = this.getRefundValue();
                    ctx.fillText(`${towerTypes[this.type].name}`, this.x, this.y - 55);
                    ctx.fillStyle = '#FF5252';
                    ctx.font = 'bold 11px Arial';
                    ctx.fillText(`Uninstall`, this.x, this.y - 40);
                    ctx.fillStyle = '#FFD700';
                    ctx.fillText(`Refund: ${refund}G`, this.x, this.y - 27);
                } else {
                    // Show upgrade info
                    const upgradeCost = this.getUpgradeCost();
                    const canUpgrade = this.canUpgrade();
                    
                    ctx.fillText(`${towerTypes[this.type].name}`, this.x, this.y - 58);
                    ctx.font = '10px Arial';
                    ctx.fillStyle = '#AAA';
                    ctx.fillText(`Level ${this.level}/${this.maxLevel}`, this.x, this.y - 44);
                    
                    if (canUpgrade) {
                        ctx.font = 'bold 11px Arial';
                        ctx.fillStyle = gold >= upgradeCost ? '#4CAF50' : '#F44336';
                        ctx.fillText(`Upgrade: ${upgradeCost}G`, this.x, this.y - 28);
                    } else {
                        ctx.font = 'bold 11px Arial';
                        ctx.fillStyle = '#FFD700';
                        ctx.fillText(`MAX LEVEL`, this.x, this.y - 28);
                    }
                }
            }
            
            // Draw tower
            ctx.fillStyle = towerTypes[this.type].color;
            ctx.fillRect(
                this.gridX * GRID_SIZE + 5,
                this.gridY * GRID_SIZE + 5,
                GRID_SIZE - 10,
                GRID_SIZE - 10
            );
            
            // Draw red overlay in demolish mode when hovered
            if (demolishMode && hoveredCell.x === this.gridX && hoveredCell.y === this.gridY) {
                ctx.fillStyle = 'rgba(244, 67, 54, 0.5)';
                ctx.fillRect(
                    this.gridX * GRID_SIZE + 5,
                    this.gridY * GRID_SIZE + 5,
                    GRID_SIZE - 10,
                    GRID_SIZE - 10
                );
            }
            
            // Draw level indicator
            ctx.fillStyle = '#FFF';
            ctx.font = 'bold 14px Arial';
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.fillText(this.level, this.x, this.y);
            
            // Draw max level indicator (star)
            if (this.level >= this.maxLevel) {
                ctx.fillStyle = '#FFD700';
                ctx.font = '16px Arial';
                ctx.fillText('★', this.x, this.y - 15);
            }
            
            // Draw barrel pointing at target
            if (this.target) {
                const angle = Math.atan2(this.target.y - this.y, this.target.x - this.x);
                ctx.save();
                ctx.translate(this.x, this.y);
                ctx.rotate(angle);
                ctx.fillStyle = '#333';
                ctx.fillRect(0, -3, 15, 6);
                ctx.restore();
            }
        }
    }
    
    class Enemy {
        constructor(type, waveMultiplier = 1) {
            this.type = type;
            this.maxHealth = Math.round(enemyTypes[type].health * waveMultiplier);
            this.health = this.maxHealth;
            // Speed increases slightly with waves (max 50% faster at wave 20)
            const speedMultiplier = 1 + Math.min(wave * 0.025, 0.5);
            this.speed = enemyTypes[type].speed * speedMultiplier;
            this.reward = Math.round(enemyTypes[type].reward * waveMultiplier);
            this.pathIndex = 0;
            this.progress = 0;
            this.x = path[0].x * GRID_SIZE;
            this.y = path[0].y * GRID_SIZE;
            this.dead = false;
            this.reachedEnd = false;
        }
        
        update(deltaTime) {
            if (this.dead || this.reachedEnd) return;
            
            const currentPoint = path[this.pathIndex];
            const nextPoint = path[this.pathIndex + 1];
            
            if (!nextPoint) {
                this.reachedEnd = true;
                lives--;
                updateStats();
                return;
            }
            
            const dx = nextPoint.x * GRID_SIZE - currentPoint.x * GRID_SIZE;
            const dy = nextPoint.y * GRID_SIZE - currentPoint.y * GRID_SIZE;
            const distance = Math.sqrt(dx * dx + dy * dy);
            
            this.progress += this.speed * deltaTime / 16.67;
            
            if (this.progress >= distance) {
                this.progress = 0;
                this.pathIndex++;
                if (this.pathIndex >= path.length - 1) {
                    this.reachedEnd = true;
                    lives--;
                    updateStats();
                }
            } else {
                this.x = currentPoint.x * GRID_SIZE + (dx / distance) * this.progress;
                this.y = currentPoint.y * GRID_SIZE + (dy / distance) * this.progress;
            }
        }
        
        takeDamage(damage) {
            this.health -= damage;
            if (this.health <= 0) {
                this.dead = true;
                gold += this.reward;
                score += this.reward;
                enemiesKilled++;
                
                // Spawn particles
                for (let i = 0; i < 10; i++) {
                    particles.push(new Particle(this.x, this.y, enemyTypes[this.type].color));
                }
                
                updateStats();
            }
        }
        
        draw() {
            if (this.dead || this.reachedEnd) return;
            
            // Draw virus (mit Spikes für gefährlicheres Aussehen)
            ctx.fillStyle = enemyTypes[this.type].color;
            ctx.beginPath();
            ctx.arc(this.x, this.y, 12, 0, Math.PI * 2);
            ctx.fill();
            
            // Draw spikes (für Virus-Look)
            ctx.fillStyle = enemyTypes[this.type].color;
            for (let i = 0; i < 8; i++) {
                const angle = (i / 8) * Math.PI * 2;
                ctx.beginPath();
                ctx.moveTo(this.x + Math.cos(angle) * 10, this.y + Math.sin(angle) * 10);
                ctx.lineTo(this.x + Math.cos(angle) * 16, this.y + Math.sin(angle) * 16);
                ctx.lineWidth = 2;
                ctx.strokeStyle = enemyTypes[this.type].color;
                ctx.stroke();
            }
            
            // Draw health bar
            const barWidth = 24;
            const barHeight = 4;
            ctx.fillStyle = '#1a1a1a';
            ctx.fillRect(this.x - barWidth / 2, this.y - 22, barWidth, barHeight);
            ctx.fillStyle = this.health / this.maxHealth > 0.3 ? '#4CAF50' : '#F44336';
            ctx.fillRect(this.x - barWidth / 2, this.y - 22, barWidth * (this.health / this.maxHealth), barHeight);
            
            // Draw virus type label
            ctx.fillStyle = '#FFF';
            ctx.font = 'bold 9px Arial';
            ctx.textAlign = 'center';
            ctx.shadowColor = '#000';
            ctx.shadowBlur = 3;
            ctx.fillText(enemyTypes[this.type].name, this.x, this.y + 24);
            ctx.shadowBlur = 0;
        }
    }
    
    class Projectile {
        constructor(x, y, target, damage, color, splash = false, splashRadius = 0) {
            this.x = x;
            this.y = y;
            this.target = target;
            this.damage = damage;
            this.color = color;
            this.speed = 5;
            this.dead = false;
            this.splash = splash;
            this.splashRadius = splashRadius;
        }
        
        update() {
            if (this.dead || this.target.dead || this.target.reachedEnd) {
                this.dead = true;
                return;
            }
            
            const dx = this.target.x - this.x;
            const dy = this.target.y - this.y;
            const distance = Math.sqrt(dx * dx + dy * dy);
            
            if (distance < this.speed) {
                if (this.splash) {
                    // Splash damage
                    for (let enemy of enemies) {
                        if (enemy.dead || enemy.reachedEnd) continue;
                        const edx = enemy.x - this.target.x;
                        const edy = enemy.y - this.target.y;
                        const enemyDistance = Math.sqrt(edx * edx + edy * edy);
                        if (enemyDistance <= this.splashRadius) {
                            enemy.takeDamage(this.damage);
                        }
                    }
                } else {
                    this.target.takeDamage(this.damage);
                }
                this.dead = true;
            } else {
                this.x += (dx / distance) * this.speed;
                this.y += (dy / distance) * this.speed;
            }
        }
        
        draw() {
            if (this.dead) return;
            
            ctx.fillStyle = this.color;
            ctx.beginPath();
            ctx.arc(this.x, this.y, 4, 0, Math.PI * 2);
            ctx.fill();
        }
    }
    
    class Particle {
        constructor(x, y, color) {
            this.x = x;
            this.y = y;
            this.color = color;
            this.vx = (Math.random() - 0.5) * 4;
            this.vy = (Math.random() - 0.5) * 4;
            this.life = 30;
            this.dead = false;
        }
        
        update() {
            this.x += this.vx;
            this.y += this.vy;
            this.life--;
            if (this.life <= 0) {
                this.dead = true;
            }
        }
        
        draw() {
            if (this.dead) return;
            
            ctx.globalAlpha = this.life / 30;
            ctx.fillStyle = this.color;
            ctx.fillRect(this.x - 2, this.y - 2, 4, 4);
            ctx.globalAlpha = 1;
        }
    }
    
    function drawGrid() {
        // Draw path
        ctx.strokeStyle = '#444';
        ctx.lineWidth = GRID_SIZE;
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';
        ctx.beginPath();
        ctx.moveTo(path[0].x * GRID_SIZE, path[0].y * GRID_SIZE);
        for (let i = 1; i < path.length; i++) {
            ctx.lineTo(path[i].x * GRID_SIZE, path[i].y * GRID_SIZE);
        }
        ctx.stroke();
        
        // Draw grid lines
        ctx.strokeStyle = '#222';
        ctx.lineWidth = 1;
        for (let i = 0; i <= COLS; i++) {
            ctx.beginPath();
            ctx.moveTo(i * GRID_SIZE, 0);
            ctx.lineTo(i * GRID_SIZE, ROWS * GRID_SIZE);
            ctx.stroke();
        }
        for (let i = 0; i <= ROWS; i++) {
            ctx.beginPath();
            ctx.moveTo(0, i * GRID_SIZE);
            ctx.lineTo(COLS * GRID_SIZE, i * GRID_SIZE);
            ctx.stroke();
        }
        
        // Highlight hovered cell
        if (hoveredCell.x >= 0 && hoveredCell.y >= 0 && selectedTowerType) {
            const canPlace = canPlaceTower(hoveredCell.x, hoveredCell.y);
            ctx.fillStyle = canPlace ? 'rgba(76, 175, 80, 0.3)' : 'rgba(244, 67, 54, 0.3)';
            ctx.fillRect(hoveredCell.x * GRID_SIZE, hoveredCell.y * GRID_SIZE, GRID_SIZE, GRID_SIZE);
            
            // Draw range preview
            if (canPlace) {
                ctx.beginPath();
                ctx.arc(
                    hoveredCell.x * GRID_SIZE + GRID_SIZE / 2,
                    hoveredCell.y * GRID_SIZE + GRID_SIZE / 2,
                    towerTypes[selectedTowerType].range,
                    0,
                    Math.PI * 2
                );
                ctx.strokeStyle = 'rgba(255, 255, 255, 0.3)';
                ctx.lineWidth = 2;
                ctx.stroke();
            }
        }
    }
    
    function canPlaceTower(gridX, gridY) {
        // Check if on path
        for (let i = 0; i < path.length - 1; i++) {
            const p1 = path[i];
            const p2 = path[i + 1];
            
            if (p1.x === p2.x) { // Vertical segment
                const minY = Math.min(p1.y, p2.y);
                const maxY = Math.max(p1.y, p2.y);
                if (gridX === p1.x && gridY >= minY && gridY <= maxY) {
                    return false;
                }
            } else { // Horizontal segment
                const minX = Math.min(p1.x, p2.x);
                const maxX = Math.max(p1.x, p2.x);
                if (gridY === p1.y && gridX >= minX && gridX <= maxX) {
                    return false;
                }
            }
        }
        
        // Check if tower already exists
        for (let tower of towers) {
            if (tower.gridX === gridX && tower.gridY === gridY) {
                return false;
            }
        }
        
        return true;
    }
    
    function spawnWave() {
        wave++;
        // Exponential difficulty increase: health and damage scale faster
        const waveMultiplier = 1 + (wave - 1) * 0.3 + Math.pow(wave - 1, 1.2) * 0.05;
        
        // More enemies per wave (exponential growth)
        let enemyCount = Math.floor(5 + wave * 2.5 + Math.pow(wave, 1.3) * 0.5);
        
        // Faster spawning at higher waves
        let baseSpawnDelay = Math.max(300, 800 - wave * 15);
        let spawnDelay = 0;
        
        for (let i = 0; i < enemyCount; i++) {
            setTimeout(() => {
                if (!gameRunning) return;
                
                let enemyType;
                // Difficulty-based enemy distribution
                const rand = Math.random();
                
                if (wave >= 15) {
                    // Wave 15+: Lots of bosses and tanks
                    if (rand < 0.25) {
                        enemyType = 'boss';
                    } else if (rand < 0.55) {
                        enemyType = 'tank';
                    } else if (rand < 0.80) {
                        enemyType = 'fast';
                    } else {
                        enemyType = 'basic';
                    }
                } else if (wave >= 10) {
                    // Wave 10-14: More bosses appear
                    if (rand < 0.15) {
                        enemyType = 'boss';
                    } else if (rand < 0.45) {
                        enemyType = 'tank';
                    } else if (rand < 0.70) {
                        enemyType = 'fast';
                    } else {
                        enemyType = 'basic';
                    }
                } else if (wave >= 5) {
                    // Wave 5-9: Tanks and fast enemies
                    if (rand < 0.05) {
                        enemyType = 'boss';
                    } else if (rand < 0.30) {
                        enemyType = 'tank';
                    } else if (rand < 0.60) {
                        enemyType = 'fast';
                    } else {
                        enemyType = 'basic';
                    }
                } else if (wave >= 3) {
                    // Wave 3-4: Some fast enemies
                    if (rand < 0.40) {
                        enemyType = 'fast';
                    } else {
                        enemyType = 'basic';
                    }
                } else {
                    // Wave 1-2: Mostly basic
                    if (rand < 0.20) {
                        enemyType = 'fast';
                    } else {
                        enemyType = 'basic';
                    }
                }
                
                enemies.push(new Enemy(enemyType, waveMultiplier));
            }, spawnDelay);
            
            spawnDelay += baseSpawnDelay;
        }
        
        updateStats();
    }
    
    function updateStats() {
        // Round values to prevent floating-point precision errors
        gold = Math.round(gold);
        score = Math.round(score);
        
        updateElement('td-gold', gold);
        updateElement('td-lives', lives);
        updateElement('td-wave', wave);
        updateElement('td-score', score);
        updateElement('td-kills', enemiesKilled);
        
        // Update mobile stats too
        updateElement('td-gold-mobile', gold);
        updateElement('td-lives-mobile', lives);
        updateElement('td-wave-mobile', wave);
        updateElement('td-score-mobile', score);
        updateElement('td-kills-mobile', enemiesKilled);
        
        if (lives <= 0) {
            gameOver();
        }
    }
    
    function updateElement(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = value;
        }
    }
    
    function gameOver() {
        gameRunning = false;
        cancelAnimationFrame(animationId);
        
        ctx.fillStyle = 'rgba(0, 0, 0, 0.9)';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        
        // Draw red warning effect
        ctx.fillStyle = 'rgba(211, 47, 47, 0.3)';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        
        ctx.fillStyle = '#FFF';
        ctx.font = 'bold 48px Arial';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.shadowColor = '#F44336';
        ctx.shadowBlur = 15;
        ctx.fillText('SYSTEM COMPROMISED', canvas.width / 2, canvas.height / 2 - 40);
        ctx.shadowBlur = 0;
        
        ctx.font = '20px Arial';
        ctx.fillStyle = '#FF5252';
        ctx.fillText('Your network has been infected!', canvas.width / 2, canvas.height / 2 + 10);
        
        ctx.fillStyle = '#FFF';
        ctx.font = '24px Arial';
        ctx.fillText(`Final Score: ${score}`, canvas.width / 2, canvas.height / 2 + 50);
        ctx.fillText(`Wave: ${wave}`, canvas.width / 2, canvas.height / 2 + 85);
        ctx.fillText(`Viruses Removed: ${enemiesKilled}`, canvas.width / 2, canvas.height / 2 + 120);
    }
    
    let lastTimestamp = 0;
    function gameLoop(timestamp) {
        if (!gameRunning) return;
        
        if (gamePaused) {
            animationId = requestAnimationFrame(gameLoop);
            return;
        }
        
        const deltaTime = timestamp - lastTimestamp;
        lastTimestamp = timestamp;
        
        // Clear canvas
        ctx.fillStyle = '#1a1a1a';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        
        // Draw grid
        drawGrid();
        
        // Update and draw towers
        for (let tower of towers) {
            tower.update(timestamp);
            tower.draw();
        }
        
        // Update and draw enemies
        enemies = enemies.filter(enemy => !enemy.dead && !enemy.reachedEnd);
        for (let enemy of enemies) {
            enemy.update(deltaTime);
            enemy.draw();
        }
        
        // Update and draw projectiles
        projectiles = projectiles.filter(projectile => !projectile.dead);
        for (let projectile of projectiles) {
            projectile.update();
            projectile.draw();
        }
        
        // Update and draw particles
        particles = particles.filter(particle => !particle.dead);
        for (let particle of particles) {
            particle.update();
            particle.draw();
        }
        
        // Check if wave is complete
        if (enemies.length === 0 && gameRunning) {
            setTimeout(() => {
                if (gameRunning && enemies.length === 0) {
                    spawnWave();
                }
            }, 3000);
        }
        
        animationId = requestAnimationFrame(gameLoop);
    }
    
    function handleCanvasClick(event) {
        if (!gameRunning || gamePaused || !event) return;
        
        try {
            const rect = canvas.getBoundingClientRect();
            const x = event.clientX - rect.left;
            const y = event.clientY - rect.top;
            
            // Validate coordinates are within canvas bounds
            if (x < 0 || x > canvas.width || y < 0 || y > canvas.height) {
                return;
            }
            
            const gridX = Math.floor(x / GRID_SIZE);
            const gridY = Math.floor(y / GRID_SIZE);
            
            // Validate grid coordinates
            if (gridX < 0 || gridX >= COLS || gridY < 0 || gridY >= ROWS) {
                return;
            }
            
            if (demolishMode) {
                // Check if clicking on tower to demolish
                for (let i = 0; i < towers.length; i++) {
                    const tower = towers[i];
                    if (tower.gridX === gridX && tower.gridY === gridY) {
                        // Get refund
                        const refund = tower.getRefundValue();
                        gold = Math.round(gold + refund);
                        
                        // Show demolish feedback
                        for (let j = 0; j < 20; j++) {
                            particles.push(new Particle(tower.x, tower.y, '#F44336'));
                        }
                        
                        // Remove tower
                        towers.splice(i, 1);
                        updateStats();
                        break;
                    }
                }
            } else if (selectedTowerType && canPlaceTower(gridX, gridY)) {
                // Validate tower type exists
                if (!towerTypes.hasOwnProperty(selectedTowerType)) {
                    console.error('Invalid tower type:', selectedTowerType);
                    return;
                }
                
                // Try to place tower
                const cost = towerTypes[selectedTowerType].cost;
                if (gold >= cost && cost > 0) {
                    towers.push(new Tower(gridX, gridY, selectedTowerType));
                    gold = Math.round(gold - cost);
                    updateStats();
                }
            } else {
                // Check if clicking on existing tower to upgrade
                for (let tower of towers) {
                    if (tower.gridX === gridX && tower.gridY === gridY) {
                        if (tower.canUpgrade()) {
                            const upgradeCost = tower.getUpgradeCost();
                            if (gold >= upgradeCost && upgradeCost > 0) {
                                tower.level++;
                                gold = Math.round(gold - upgradeCost);
                                updateStats();
                                
                                // Show upgrade feedback
                                for (let i = 0; i < 15; i++) {
                                    particles.push(new Particle(tower.x, tower.y, '#FFD700'));
                                }
                            }
                        }
                        break;
                    }
                }
            }
        } catch (error) {
            console.error('Tower Defense: Error handling click:', error);
        }
    }
    
    function handleCanvasMove(event) {
        if (!gameRunning || !event) return;
        
        try {
            const rect = canvas.getBoundingClientRect();
            const x = event.clientX - rect.left;
            const y = event.clientY - rect.top;
            
            // Validate coordinates
            if (x < 0 || x > canvas.width || y < 0 || y > canvas.height) {
                hoveredCell.x = -1;
                hoveredCell.y = -1;
                return;
            }
            
            const gridX = Math.floor(x / GRID_SIZE);
            const gridY = Math.floor(y / GRID_SIZE);
            
            // Validate grid coordinates
            if (gridX >= 0 && gridX < COLS && gridY >= 0 && gridY < ROWS) {
                hoveredCell.x = gridX;
                hoveredCell.y = gridY;
            } else {
                hoveredCell.x = -1;
                hoveredCell.y = -1;
            }
        } catch (error) {
            console.error('Tower Defense: Error handling mouse move:', error);
        }
    }
    
    // Public API
    window.towerdefense = {
        startGame: function() {
            try {
                // Stop any running game first
                if (gameRunning) {
                    gameRunning = false;
                    gamePaused = false;
                    if (animationId) {
                        cancelAnimationFrame(animationId);
                    }
                }
                
                canvas = document.getElementById('towerdefenseCanvas');
                if (!canvas || !canvas.getContext) {
                    console.error('Tower Defense: Canvas not found or not supported');
                    return;
                }

                ctx = canvas.getContext('2d');
                if (!ctx) {
                    console.error('Tower Defense: Could not get 2D context');
                    return;
                }

                // Remove old event listeners before adding new ones
                canvas.removeEventListener('click', handleCanvasClick);
                canvas.removeEventListener('mousemove', handleCanvasMove);

                // Reset game state to safe default values (validated)
                gold = Math.max(0, 200);
                lives = Math.max(0, 20);
                wave = 0;
                score = 0;
                enemiesKilled = 0;
                towers = [];
                enemies = [];
                projectiles = [];
                particles = [];
                selectedTowerType = null;
                hoveredCell = { x: -1, y: -1 };
                demolishMode = false;

                gameRunning = true;
                gamePaused = false;
                lastTimestamp = 0;

                updateStats();

                // Add event listeners
                canvas.addEventListener('click', handleCanvasClick);
                canvas.addEventListener('mousemove', handleCanvasMove);

                // Start first wave after 2 seconds
                setTimeout(() => {
                    if (gameRunning) {
                        spawnWave();
                    }
                }, 2000);

                animationId = requestAnimationFrame(gameLoop);
            } catch (error) {
                console.error('Tower Defense: Error starting game:', error);
                gameRunning = false;
            }
        },
        

        togglePause: function() {
            try {
                if (!gameRunning || !canvas || !ctx) return;
                
                const wasPaused = gamePaused;
                gamePaused = !gamePaused;
                
                if (gamePaused) {
                    // Draw pause overlay
                    ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
                    ctx.fillRect(0, 0, canvas.width, canvas.height);
                    ctx.fillStyle = '#FFF';
                    ctx.font = 'bold 48px Arial';
                    ctx.textAlign = 'center';
                    ctx.textBaseline = 'middle';
                    ctx.shadowColor = '#000';
                    ctx.shadowBlur = 10;
                    ctx.fillText('SYSTEM PAUSED', canvas.width / 2, canvas.height / 2);
                    ctx.shadowBlur = 0;
                    ctx.font = '20px Arial';
                    ctx.fillStyle = '#AAA';
                    ctx.fillText('Press Pause to continue', canvas.width / 2, canvas.height / 2 + 40);
                } else if (wasPaused) {
                    // Resume - reset timestamp to prevent large deltaTime jump
                    lastTimestamp = performance.now();
                }
            } catch (error) {
                console.error('Tower Defense: Error toggling pause:', error);
            }
        },
        
        selectTower: function(type) {
            try {
                // Validate tower type to prevent injection
                if (typeof type !== 'string') {
                    console.error('Tower Defense: Invalid tower type (not a string)');
                    return;
                }
                
                // Whitelist validation - only allow valid tower types
                if (!towerTypes.hasOwnProperty(type)) {
                    console.error('Tower Defense: Invalid tower type:', type);
                    return;
                }
                
                selectedTowerType = type;
                demolishMode = false;
            } catch (error) {
                console.error('Tower Defense: Error selecting tower:', error);
            }
        },
        
        toggleDemolish: function() {
            try {
                demolishMode = !demolishMode;
                if (demolishMode) {
                    selectedTowerType = null;
                }
                return demolishMode;
            } catch (error) {
                console.error('Tower Defense: Error toggling demolish:', error);
                return false;
            }
        }
    };
})();

