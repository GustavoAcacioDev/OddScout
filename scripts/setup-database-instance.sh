#!/bin/bash

# Script para configurar a instância do banco de dados Oracle Cloud
set -e

echo "🗄️ Configurando instância do banco de dados Oracle Cloud..."

# Atualizar sistema
sudo yum update -y

# Instalar Docker
echo "📦 Instalando Docker..."
sudo yum install -y docker
sudo systemctl start docker
sudo systemctl enable docker
sudo usermod -aG docker opc

# Instalar Docker Compose
echo "📦 Instalando Docker Compose..."
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Criar diretórios
echo "📁 Criando estrutura de diretórios..."
mkdir -p ~/oddscout/backup

# Configurar firewall
echo "🔥 Configurando firewall..."
sudo firewall-cmd --permanent --add-port=1433/tcp
sudo firewall-cmd --reload

echo "✅ Instância do banco configurada!"
echo ""
echo "🔄 Próximos passos:"
echo "1. Faça upload do docker-compose.database.yml"
echo "2. Execute: cd ~/oddscout && docker-compose -f docker-compose.database.yml up -d"