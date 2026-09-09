using System;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace GhostArena
{
    public sealed class CharactersFactory
    {
        private readonly ControllersFactory _controllersFactory;
        private readonly ControllersUpdateService _controllers;
        private readonly GameObject _playerPrefab;
        private readonly GameObject _enemyPrefab;
        private readonly GameObject _projectilePrefab;
        private readonly Transform _runtimeRoot;
        private readonly Camera _gameplayCamera;
        private readonly Vector2 _arenaHalfExtents;

        public CharactersFactory(
            ControllersFactory controllersFactory,
            ControllersUpdateService controllers,
            GameObject playerPrefab,
            GameObject enemyPrefab,
            GameObject projectilePrefab,
            Transform runtimeRoot,
            Camera gameplayCamera,
            Vector2 arenaHalfExtents)
        {
            _controllersFactory = controllersFactory;
            _controllers = controllers;
            _playerPrefab = playerPrefab;
            _enemyPrefab = enemyPrefab;
            _projectilePrefab = projectilePrefab;
            _runtimeRoot = runtimeRoot;
            _gameplayCamera = gameplayCamera;
            _arenaHalfExtents = arenaHalfExtents;
        }

        public Character CreatePlayer(
            Transform spawnPoint,
            GameplaySettings settings,
            GhostVisualSettings visualSettings)
        {
            if (spawnPoint == null)
            {
                throw new ArgumentNullException(nameof(spawnPoint));
            }

            GameObject characterObject = Object.Instantiate(
                _playerPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                _runtimeRoot);

            try
            {
                Character character = characterObject.GetComponent<Character>();
                Rigidbody body = characterObject.GetComponent<Rigidbody>();
                WeaponMount weaponMount = characterObject.GetComponent<WeaponMount>();
                GhostVisual visual = characterObject.GetComponentInChildren<GhostVisual>(true);

                if (character == null || body == null || weaponMount == null || visual == null)
                {
                    throw new InvalidOperationException("Player prefab components are not configured.");
                }

                if (weaponMount.Muzzle == null)
                {
                    throw new InvalidOperationException("Player weapon muzzle is not configured.");
                }

                RigidbodyDirectionalMover mover = new RigidbodyDirectionalMover(
                    body,
                    settings.PlayerMovementSpeed);
                RigidbodyDirectionalRotator rotator = new RigidbodyDirectionalRotator(body);
                character.Initialize(
                    new Health(settings.PlayerMaximumHealth),
                    mover,
                    null,
                    rotator);
                Weapon weapon = new Weapon(
                    character,
                    weaponMount.Muzzle,
                    _projectilePrefab,
                    _runtimeRoot,
                    settings.Projectile);
                character.BindWeapon(weapon);
                Controller controller = _controllersFactory.CreatePlayer(character, _gameplayCamera);
                character.BindController(controller);
                _controllers.Add(controller);
                visual.Initialize(visualSettings);
                return character;
            }
            catch
            {
                Object.Destroy(characterObject);
                throw;
            }
        }

        public Character CreateEnemy(
            Transform spawnPoint,
            EnemySettings settings,
            GhostVisualSettings visualSettings)
        {
            if (spawnPoint == null)
            {
                throw new ArgumentNullException(nameof(spawnPoint));
            }

            GameObject characterObject = Object.Instantiate(
                _enemyPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                _runtimeRoot);

            try
            {
                Character character = characterObject.GetComponent<Character>();
                NavMeshAgent agent = characterObject.GetComponent<NavMeshAgent>();
                ContactDamage contactDamage = characterObject.GetComponent<ContactDamage>();
                GhostVisual visual = characterObject.GetComponentInChildren<GhostVisual>(true);

                if (character == null || agent == null || contactDamage == null || visual == null)
                {
                    throw new InvalidOperationException("Enemy prefab components are not configured.");
                }

                if (agent.isOnNavMesh == false)
                {
                    throw new InvalidOperationException("Enemy navigation agent is not placed on a NavMesh.");
                }

                float rotationSpeed = agent.angularSpeed;
                AgentMover mover = new AgentMover(agent, settings.MovementSpeed);
                TransformDirectionalRotator rotator =
                    new TransformDirectionalRotator(character.transform, rotationSpeed);
                character.Initialize(
                    new Health(settings.MaximumHealth),
                    null,
                    mover,
                    rotator);
                Controller controller = _controllersFactory.CreateEnemy(
                    character,
                    _arenaHalfExtents,
                    settings.DirectionInterval,
                    settings.MovementSpeed * settings.DirectionInterval);
                character.BindController(controller);
                _controllers.Add(controller);
                contactDamage.Initialize(character, settings.ContactDamage);
                visual.Initialize(visualSettings);
                return character;
            }
            catch
            {
                Object.Destroy(characterObject);
                throw;
            }
        }

    }
}
